using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.Signing;
using sh.be.applications.Base.Interfaces;
using sh.be.applications.Module.DigitalSignature.Dtos.SigningBatch;
using sh.be.applications.Module.DigitalSignature.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.DigitalSignature.Entities;
using sh.be.domain.Module.DigitalSignature.Repositories;
using sh.be.domain.Module.DigitalSignature.Services;
using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace sh.be.applications.Module.DigitalSignature.Implements
{
    public class SigningRunner : ISigningRunner
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IPdfSigner _pdfSigner;
        private readonly ITimestampClient _timestampClient;
        private readonly ISigningQueue _signingQueue;
        private readonly ILogger<SigningRunner> _logger;
        private readonly TimeProvider _timeProvider;
        private readonly ConcurrentDictionary<int, RunningBatch> _runningBatches = new();
        private readonly SemaphoreSlim _batchSlots = new(SigningBatchConst.MAX_RUNNING_BATCHES);
        private readonly SemaphoreSlim _prepareSlots = new(Environment.ProcessorCount);
        private readonly SemaphoreSlim _timestampSlots = new(SigningBatchConst.TSA_CONNECTIONS);
        private readonly SemaphoreSlim _finishSlots = new(SigningBatchConst.FINISH_SLOTS);

        public SigningRunner(IServiceScopeFactory serviceScopeFactory, IPdfSigner pdfSigner, ITimestampClient timestampClient, ISigningQueue signingQueue,
            ILogger<SigningRunner> logger, TimeProvider timeProvider)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _pdfSigner = pdfSigner;
            _timestampClient = timestampClient;
            _signingQueue = signingQueue;
            _logger = logger;
            _timeProvider = timeProvider;
        }

        public void Start(SigningBatch signingBatch)
        {
            _logger.LogInformation($"{nameof(Start)}, id={signingBatch.Id}");
            var running = new RunningBatch(signingBatch);
            if (_runningBatches.TryAdd(signingBatch.Id, running))
            {
                _ = Task.Run(() => RunAsync(running));
            }
        }

        public void Stop(int signingBatchId)
        {
            _logger.LogInformation($"{nameof(Stop)}, id={signingBatchId}");
            if (_runningBatches.TryGetValue(signingBatchId, out var running))
            {
                running.Cancellation.Cancel();
            }
        }

        public void EnsureNotRunning(int signingBatchId)
        {
            if (_runningBatches.ContainsKey(signingBatchId))
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningBatchRunning);
            }
        }

        public SigningBatchProgressDto? GetProgress(int signingBatchId, string? userId)
        {
            if (!_runningBatches.TryGetValue(signingBatchId, out var running) || running.CreatedBy != userId)
            {
                return null;
            }

            return new SigningBatchProgressDto
            {
                Id = signingBatchId,
                Status = SigningBatchConst.SIGNING,
                TotalFiles = running.TotalFiles,
                SignedFiles = running.SignedFiles,
                FailedFiles = running.FailedFiles,
                IsRunning = true,
            };
        }

        public async Task RecoverAsync()
        {
            _logger.LogInformation($"{nameof(RecoverAsync)}");
            using var scope = _serviceScopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ISigningBatchRepository>()
                .PauseInterruptedAsync(SigningBatchConst.PAUSED_BY_RESTART_REASON, GetVietnamTime());
        }

        private async Task RunAsync(RunningBatch running)
        {
            try
            {
                await _batchSlots.WaitAsync(running.Cancellation.Token);
                try
                {
                    await RunPipelineAsync(running);
                }
                finally
                {
                    _batchSlots.Release();
                }
            }
            catch (OperationCanceledException) when (running.Cancellation.IsCancellationRequested)
            {
                _logger.LogInformation($"{nameof(RunAsync)}, id={running.Id}, cancelled before start");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(RunAsync)}, id={running.Id}");
                await ReturnSigningFilesAsync(running);
                await StopBatchAsync(running, signingBatch => signingBatch.Fail(SigningBatchConst.FAILED_BY_SYSTEM_REASON, running.CreatedBy, GetVietnamTime()));
            }
            finally
            {
                _runningBatches.TryRemove(running.Id, out _);
                running.Cancellation.Dispose();
            }
        }

        private async Task RunPipelineAsync(RunningBatch running)
        {
            using var storageScope = _serviceScopeFactory.CreateScope();
            var fileStorage = storageScope.ServiceProvider.GetRequiredService<IFileStorage>();
            PdfSigningContextDto context;
            try
            {
                context = await CreateContextAsync(running, fileStorage);
            }
            catch (UserFriendlyException ex)
            {
                _logger.LogWarning(ex, $"{nameof(RunPipelineAsync)}, id={running.Id}, cannot create context");
                await StopBatchAsync(running, signingBatch => StopByError(signingBatch, running, ex));
                return;
            }

            using var certificate = context.Certificate;
            var token = running.Cancellation.Token;
            var claimed = Channel.CreateBounded<SigningBatchFile>(SigningBatchConst.REQUEST_BATCH_SIZE);
            var prepared = Channel.CreateBounded<PreparedFile>(SigningBatchConst.PREFETCH_FILES);
            var signed = Channel.CreateBounded<SignedFile>(SigningBatchConst.REQUEST_BATCH_SIZE);

            var prepareTasks = StartWorkers(SigningBatchConst.PREPARE_WORKERS, () => PrepareAsync(running, context, fileStorage, claimed.Reader, prepared.Writer, token), running);
            var signTasks = StartWorkers(SigningBatchConst.REQUEST_BATCH_SIZE, () => SignAsync(running, prepared.Reader, signed.Writer, token), running);
            var finishTasks = StartWorkers(SigningBatchConst.TSA_CONNECTIONS, () => FinishAsync(running, context, fileStorage, signed.Reader, token), running);
            var tasks = new List<Task>
            {
                Guard(() => ClaimAsync(running, claimed.Writer, token), running),
                CompleteWhenDone(prepareTasks, prepared.Writer),
                CompleteWhenDone(signTasks, signed.Writer),
            };
            tasks.AddRange(prepareTasks);
            tasks.AddRange(signTasks);
            tasks.AddRange(finishTasks);

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception)
            {
            }

            var failure = tasks.Where(t => t.IsFaulted).SelectMany(t => t.Exception!.InnerExceptions).FirstOrDefault(e => e is not OperationCanceledException);
            if (failure != null)
            {
                throw failure;
            }

            if (token.IsCancellationRequested)
            {
                await ReturnSigningFilesAsync(running);
                if (running.PauseReason != null)
                {
                    await StopBatchAsync(running, signingBatch => signingBatch.Pause(running.PauseReason, running.CreatedBy, GetVietnamTime()));
                }
                return;
            }

            await StopBatchAsync(running, signingBatch => signingBatch.Complete(running.CreatedBy, GetVietnamTime()));
            _signingQueue.CloseSession(running.Id);
        }

        private async Task<PdfSigningContextDto> CreateContextAsync(RunningBatch running, IFileStorage fileStorage)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var templateRepository = scope.ServiceProvider.GetRequiredService<ITemplateRepository>();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
            var template = await templateRepository.FindByIdAsync(running.TemplateId)
                ?? throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorTemplateNotFound);

            var options = mapper.Map<PdfSigningOptionsDto>(template);
            options.Positions = mapper.Map<List<PdfSigningPositionDto>>(await templateRepository.GetPositionsAsync(template.Id));
            options.StampImage = await DownloadTemplateImageAsync(fileStorage, template.StampImageObjectKey);
            options.WetSignatureImage = await DownloadTemplateImageAsync(fileStorage, template.WetSignatureImageObjectKey);
            running.AllowSigningSignedPdf = template.AllowSigningSignedPdf;
            return _pdfSigner.CreateContext(_signingQueue.CopySessionCertificate(running.Id), options);
        }

        private static async Task<byte[]?> DownloadTemplateImageAsync(IFileStorage fileStorage, string? objectKey)
        {
            if (string.IsNullOrWhiteSpace(objectKey))
            {
                return null;
            }
            try
            {
                return await fileStorage.DownloadAsync(objectKey);
            }
            catch (UserFriendlyException)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorTemplateImageUnreadable);
            }
        }

        private async Task ClaimAsync(RunningBatch running, ChannelWriter<SigningBatchFile> writer, CancellationToken token)
        {
            try
            {
                while (true)
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var files = await scope.ServiceProvider.GetRequiredService<ISigningBatchRepository>()
                        .ClaimFilesAsync(running.Id, SigningBatchConst.REQUEST_BATCH_SIZE, running.CreatedBy, GetVietnamTime());
                    if (files.Count == 0)
                    {
                        return;
                    }
                    foreach (var file in files)
                    {
                        await writer.WriteAsync(file, token);
                    }
                }
            }
            finally
            {
                writer.TryComplete();
            }
        }

        private async Task PrepareAsync(RunningBatch running, PdfSigningContextDto context, IFileStorage fileStorage, ChannelReader<SigningBatchFile> reader,
            ChannelWriter<PreparedFile> writer, CancellationToken token)
        {
            await foreach (var file in reader.ReadAllAsync(token))
            {
                PdfPreparedSignatureDto preparedSignature;
                try
                {
                    var pdf = await fileStorage.DownloadAsync(file.SourceObjectKey, token);
                    await _prepareSlots.WaitAsync(token);
                    try
                    {
                        SigningBatchFilePolicy.EnsureSignable(_pdfSigner.HasSignature(pdf), running.AllowSigningSignedPdf);
                        var now = _timeProvider.GetUtcNow();
                        preparedSignature = _pdfSigner.Prepare(pdf, context, TimeZoneInfo.ConvertTime(now, BaseService.VietnamTimeZone).DateTime, now.UtcDateTime);
                    }
                    finally
                    {
                        _prepareSlots.Release();
                    }
                }
                catch (Exception ex) when (!token.IsCancellationRequested)
                {
                    await MarkFailedAsync(running, file, ex);
                    continue;
                }
                await writer.WriteAsync(new PreparedFile(file, preparedSignature), token);
            }
        }

        private async Task SignAsync(RunningBatch running, ChannelReader<PreparedFile> reader, ChannelWriter<SignedFile> writer, CancellationToken token)
        {
            await foreach (var item in reader.ReadAllAsync(token))
            {
                byte[] signature;
                try
                {
                    signature = await _signingQueue.RequestSignatureAsync(running.Id, item.Prepared.SignedAttributes, token);
                }
                catch (UserFriendlyException ex) when (ex.ErrorCode == ErrorCodes.DigitalSignatureErrorSigningSessionNotOpen)
                {
                    running.Pause(SigningBatchConst.PAUSED_BY_SESSION_REASON);
                    return;
                }
                catch (Exception ex) when (!token.IsCancellationRequested)
                {
                    await MarkFailedAsync(running, item.File, ex);
                    continue;
                }
                await writer.WriteAsync(new SignedFile(item.File, item.Prepared, signature), token);
            }
        }

        private async Task FinishAsync(RunningBatch running, PdfSigningContextDto context, IFileStorage fileStorage, ChannelReader<SignedFile> reader, CancellationToken token)
        {
            await foreach (var item in reader.ReadAllAsync(token))
            {
                byte[] timestampToken;
                try
                {
                    timestampToken = await RequestTimestampAsync(item.Signature, token);
                    running.ResetTimestampFailures();
                }
                catch (UserFriendlyException ex) when (ex.ErrorCode == ErrorCodes.DigitalSignatureErrorTimestampFailed)
                {
                    await MarkFailedAsync(running, item.File, ex);
                    if (running.CountTimestampFailure() >= SigningBatchConst.TSA_FAILURES_TO_PAUSE)
                    {
                        running.Pause(SigningBatchConst.PAUSED_BY_TSA_REASON);
                    }
                    continue;
                }

                try
                {
                    await _finishSlots.WaitAsync(token);
                    try
                    {
                        await SaveSignedFileAsync(running, context, fileStorage, item, timestampToken);
                    }
                    finally
                    {
                        _finishSlots.Release();
                    }
                }
                catch (Exception ex) when (!token.IsCancellationRequested)
                {
                    await MarkFailedAsync(running, item.File, ex);
                }
            }
        }

        private async Task<byte[]> RequestTimestampAsync(byte[] signature, CancellationToken token)
        {
            await _timestampSlots.WaitAsync(token);
            try
            {
                return await _timestampClient.RequestTokenAsync(signature, token);
            }
            finally
            {
                _timestampSlots.Release();
            }
        }

        private async Task SaveSignedFileAsync(RunningBatch running, PdfSigningContextDto context, IFileStorage fileStorage, SignedFile item, byte[] timestampToken)
        {
            var signedPdf = _pdfSigner.Complete(item.Prepared, item.Signature, timestampToken, context);
            var signedObjectKey = SigningBatchFilePolicy.BuildSignedObjectKey(running.Id, item.File.Order);
            using (var content = new MemoryStream(signedPdf))
            {
                await fileStorage.UploadPrivateAsync(content, signedObjectKey, SigningBatchConst.PDF_CONTENT_TYPE);
            }

            var timestampedAtUtc = _timestampClient.ReadTimeUtc(timestampToken);
            DateTime? timestampedAt = timestampedAtUtc.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(timestampedAtUtc.Value, BaseService.VietnamTimeZone) : null;
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<ISigningBatchRepository>()
                    .MarkFileSignedAsync(item.File, signedObjectKey, item.Prepared.SignedAt, timestampedAt, running.CreatedBy, GetVietnamTime());
            }
            running.CountSigned();
            await fileStorage.DeleteAsync(item.File.SourceObjectKey);
        }

        private async Task MarkFailedAsync(RunningBatch running, SigningBatchFile file, Exception exception)
        {
            _logger.LogWarning(exception, $"{nameof(MarkFailedAsync)}, id={running.Id}, fileId={file.Id}");
            using var scope = _serviceScopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ISigningBatchRepository>()
                .MarkFileFailedAsync(file, SigningBatchFilePolicy.ToErrorMessage(exception), running.CreatedBy, GetVietnamTime());
            running.CountFailed();
        }

        private async Task ReturnSigningFilesAsync(RunningBatch running)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ISigningBatchRepository>().ReturnSigningFilesAsync(running.Id, running.CreatedBy, GetVietnamTime());
        }

        private async Task StopBatchAsync(RunningBatch running, Action<SigningBatch> stop)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var signingBatchRepository = scope.ServiceProvider.GetRequiredService<ISigningBatchRepository>();
            var signingBatch = await signingBatchRepository.FindByIdAsync(running.Id);
            if (signingBatch == null)
            {
                return;
            }

            try
            {
                stop(signingBatch);
            }
            catch (UserFriendlyException ex)
            {
                _logger.LogInformation($"{nameof(StopBatchAsync)}, id={running.Id}, status={signingBatch.Status}, code={ex.ErrorCode}");
                return;
            }
            signingBatchRepository.Update(signingBatch);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        private void StopByError(SigningBatch signingBatch, RunningBatch running, UserFriendlyException exception)
        {
            if (exception.ErrorCode == ErrorCodes.DigitalSignatureErrorSigningSessionNotOpen)
            {
                signingBatch.Pause(SigningBatchConst.PAUSED_BY_SESSION_REASON, running.CreatedBy, GetVietnamTime());
                return;
            }
            signingBatch.Fail(SigningBatchFilePolicy.ToErrorMessage(exception), running.CreatedBy, GetVietnamTime());
        }

        private static List<Task> StartWorkers(int count, Func<Task> work, RunningBatch running)
        {
            return Enumerable.Range(0, count).Select(_ => Guard(work, running)).ToList();
        }

        private static async Task Guard(Func<Task> work, RunningBatch running)
        {
            try
            {
                await work();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                running.Cancellation.Cancel();
                throw;
            }
        }

        private static async Task CompleteWhenDone<T>(IEnumerable<Task> tasks, ChannelWriter<T> writer)
        {
            try
            {
                await Task.WhenAll(tasks);
            }
            finally
            {
                writer.TryComplete();
            }
        }

        private DateTime GetVietnamTime()
        {
            return TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), BaseService.VietnamTimeZone).DateTime;
        }

        private sealed record PreparedFile(SigningBatchFile File, PdfPreparedSignatureDto Prepared);

        private sealed record SignedFile(SigningBatchFile File, PdfPreparedSignatureDto Prepared, byte[] Signature);

        private sealed class RunningBatch(SigningBatch signingBatch)
        {
            private int _signedFiles = signingBatch.SignedFiles;
            private int _failedFiles = signingBatch.FailedFiles;
            private int _timestampFailures;

            public int Id { get; } = signingBatch.Id;
            public int TemplateId { get; } = signingBatch.TemplateId;
            public string? CreatedBy { get; } = signingBatch.CreatedBy;
            public int TotalFiles { get; } = signingBatch.TotalFiles;
            public int SignedFiles => Volatile.Read(ref _signedFiles);
            public int FailedFiles => Volatile.Read(ref _failedFiles);
            public bool AllowSigningSignedPdf { get; set; }
            public string? PauseReason { get; private set; }
            public CancellationTokenSource Cancellation { get; } = new();

            public void CountSigned()
            {
                Interlocked.Increment(ref _signedFiles);
            }

            public void CountFailed()
            {
                Interlocked.Increment(ref _failedFiles);
            }

            public int CountTimestampFailure()
            {
                return Interlocked.Increment(ref _timestampFailures);
            }

            public void ResetTimestampFailures()
            {
                Interlocked.Exchange(ref _timestampFailures, 0);
            }

            public void Pause(string reason)
            {
                PauseReason ??= reason;
                Cancellation.Cancel();
            }
        }
    }
}
