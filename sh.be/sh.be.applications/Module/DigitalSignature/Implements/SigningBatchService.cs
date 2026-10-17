using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.Signing;
using sh.be.applications.Base.Interfaces;
using sh.be.applications.Module.DigitalSignature.Dtos.SigningBatch;
using sh.be.applications.Module.DigitalSignature.Interfaces.Queries;
using sh.be.applications.Module.DigitalSignature.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Core.Repositories;
using sh.be.domain.Module.DigitalSignature.Entities;
using sh.be.domain.Module.DigitalSignature.Repositories;
using sh.be.domain.Module.DigitalSignature.Services;
using sh.be.shared.Constants.Core;
using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;

namespace sh.be.applications.Module.DigitalSignature.Implements
{
    public class SigningBatchService : BaseService, ISigningBatchService
    {
        private readonly ISigningBatchRepository _signingBatchRepository;
        private readonly ISigningBatchQuery _signingBatchQuery;
        private readonly ITemplateRepository _templateRepository;
        private readonly IFileStorage _fileStorage;
        private readonly ICertificateValidator _certificateValidator;
        private readonly ISigningQueue _signingQueue;
        private readonly ISigningRunner _signingRunner;

        public SigningBatchService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<SigningBatchService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            ISigningBatchRepository signingBatchRepository, ISigningBatchQuery signingBatchQuery, ITemplateRepository templateRepository, IFileStorage fileStorage,
            ICertificateValidator certificateValidator, ISigningQueue signingQueue, ISigningRunner signingRunner
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _signingBatchRepository = signingBatchRepository;
            _signingBatchQuery = signingBatchQuery;
            _templateRepository = templateRepository;
            _fileStorage = fileStorage;
            _certificateValidator = certificateValidator;
            _signingQueue = signingQueue;
            _signingRunner = signingRunner;
        }

        public async Task<FindSigningBatchByIdDto?> FindRunning()
        {
            _logger.LogInformation($"{nameof(FindRunning)}");
            return await _signingBatchQuery.FindRunning(getCurrentUserId());
        }

        public async Task<List<GetListSigningBatchFileDto>> GetFiles(int id)
        {
            _logger.LogInformation($"{nameof(GetFiles)}, id={id}");
            await FindOwnedSigningBatchAsync(id);
            return await _signingBatchQuery.GetFiles(id);
        }

        public async Task<FindSigningBatchByIdDto> Create(CreateSigningBatchDto dto)
        {
            _logger.LogInformation($"{nameof(Create)}, dto={JsonSerializer.Serialize(dto)}");
            var template = await _templateRepository.FindByIdAsync(dto.TemplateId)
                ?? throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorTemplateNotFound);

            var signingBatch = SigningBatch.Create(template.Id, getCurrentUserId(), GetVietnamTime());
            signingBatch.NameSignedFolder(dto.SignedFolderName);

            _signingBatchRepository.Add(signingBatch);
            AddAuditLog(AuditLogConstants.ACTION_SIGNING_BATCH_CREATE, AuditLogConstants.ACTION_TYPE_CREATE, $"Tạo lô ký {signingBatch.SignedFolderName}",
                null, new { signingBatch.TemplateId, signingBatch.SignedFolderName });
            await _unitOfWork.SaveChangesAsync();

            return await FindById(signingBatch.Id);
        }

        public async Task<FindSigningBatchByIdDto> AddFiles(int id, AddFilesSigningBatchDto dto)
        {
            _logger.LogInformation($"{nameof(AddFiles)}, id={id}, fileCount={dto.Files.Count}");
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();

            var signingBatch = await FindOwnedSigningBatchAsync(id);
            signingBatch.EnsureAcceptUpload();
            SigningBatchFilePolicy.EnsureUploadCount(dto.Files.Count);

            var fileNames = (await _signingBatchRepository.GetFileNamesAsync(id)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var order = await _signingBatchRepository.GetLastFileOrderAsync(id);
            var uploads = new List<(IFormFile FormFile, SigningBatchFile File)>();
            foreach (var formFile in dto.Files.Where(f => SigningBatchFilePolicy.IsAcceptedFile(f.FileName, f.Length) && fileNames.Add(f.FileName)))
            {
                order++;
                var sourceObjectKey = SigningBatchFilePolicy.BuildSourceObjectKey(id, order);
                uploads.Add((formFile, signingBatch.CreateFile(order, formFile.FileName, sourceObjectKey, null, currentUserId, now)));
            }

            await Parallel.ForEachAsync(uploads, new ParallelOptions { MaxDegreeOfParallelism = SigningBatchConst.UPLOAD_PARALLELISM },
                async (upload, _) =>
                {
                    await using var content = upload.FormFile.OpenReadStream();
                    await _fileStorage.UploadPrivateAsync(content, upload.File.SourceObjectKey, SigningBatchConst.PDF_CONTENT_TYPE);
                });

            _signingBatchRepository.AddFiles(uploads.Select(u => u.File).ToList());
            _signingBatchRepository.Update(signingBatch);
            AddAuditLog(AuditLogConstants.ACTION_SIGNING_BATCH_ADD_FILES, AuditLogConstants.ACTION_TYPE_CREATE, $"Thêm {uploads.Count} file vào lô ký {signingBatch.SignedFolderName}",
                null, new { signingBatch.Id, AddedFiles = uploads.Count, signingBatch.TotalFiles });
            await _unitOfWork.SaveChangesAsync();

            return await FindById(id);
        }

        public async Task<FindSigningBatchByIdDto> RemoveFile(int id, int fileId)
        {
            _logger.LogInformation($"{nameof(RemoveFile)}, id={id}, fileId={fileId}");
            var signingBatch = await FindOwnedSigningBatchAsync(id);
            var file = await _signingBatchRepository.FindFileAsync(id, fileId)
                ?? throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningBatchFileNotFound);

            signingBatch.RemoveFile(file, getCurrentUserId(), GetVietnamTime());

            _signingBatchRepository.Update(signingBatch);
            AddAuditLog(AuditLogConstants.ACTION_SIGNING_BATCH_REMOVE_FILE, AuditLogConstants.ACTION_TYPE_DELETE, $"Xóa file {file.FileName} khỏi lô ký {signingBatch.SignedFolderName}",
                new { signingBatch.Id, FileId = file.Id, file.FileName }, null);
            await _unitOfWork.SaveChangesAsync();

            await _fileStorage.DeleteAsync(file.SourceObjectKey);
            return await FindById(id);
        }

        public async Task<FindSigningBatchByIdDto> OpenSession(int id, OpenSessionSigningBatchDto dto)
        {
            _logger.LogInformation($"{nameof(OpenSession)}, id={id}");
            var signingBatch = await FindOwnedSigningBatchAsync(id);
            var certificate = _certificateValidator.LoadTrusted(dto.CertificateBase64, _timeProvider.GetUtcNow().UtcDateTime);
            var oldData = new { signingBatch.Id, signingBatch.CertificateThumbprint };

            signingBatch.ChangeCertificate(certificate.Thumbprint, getCurrentUserId(), GetVietnamTime());
            _signingQueue.OpenSession(id, certificate);

            _signingBatchRepository.Update(signingBatch);
            AddAuditLog(AuditLogConstants.ACTION_SIGNING_BATCH_OPEN_SESSION, AuditLogConstants.ACTION_TYPE_UPDATE, $"Mở phiên ký lô {signingBatch.SignedFolderName}",
                oldData, new { signingBatch.Id, signingBatch.CertificateThumbprint, certificate.Subject });
            await _unitOfWork.SaveChangesAsync();

            return await FindById(id);
        }

        public async Task CloseSession(int id)
        {
            _logger.LogInformation($"{nameof(CloseSession)}, id={id}");
            await FindOwnedSigningBatchAsync(id);
            _signingQueue.CloseSession(id);
        }

        public async Task<FindSigningBatchByIdDto> Start(int id)
        {
            _logger.LogInformation($"{nameof(Start)}, id={id}");
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();
            var signingBatch = await FindOwnedSigningBatchAsync(id);
            _signingRunner.EnsureNotRunning(id);
            var oldData = new { signingBatch.Id, signingBatch.Status, signingBatch.FailedFiles };

            var failedFiles = await _signingBatchRepository.GetFailedFilesAsync(id);
            failedFiles.ForEach(file => file.Retry(currentUserId, now));
            signingBatch.RequeueFailedFiles(failedFiles.Count);
            var pendingFiles = await _signingBatchRepository.CountPendingFilesAsync(id) + failedFiles.Count;
            signingBatch.Start(_signingQueue.GetSessionThumbprint(id), pendingFiles, currentUserId, now);

            _signingBatchRepository.Update(signingBatch);
            AddAuditLog(AuditLogConstants.ACTION_SIGNING_BATCH_START, AuditLogConstants.ACTION_TYPE_UPDATE, $"Bắt đầu ký lô {signingBatch.SignedFolderName}",
                oldData, new { signingBatch.Id, signingBatch.Status, RequeuedFiles = failedFiles.Count, PendingFiles = pendingFiles });
            await _unitOfWork.SaveChangesAsync();

            _signingRunner.Start(signingBatch);
            return await FindById(id);
        }

        public async Task<FindSigningBatchByIdDto> Pause(int id)
        {
            _logger.LogInformation($"{nameof(Pause)}, id={id}");
            var signingBatch = await FindOwnedSigningBatchAsync(id);
            signingBatch.Pause(SigningBatchConst.PAUSED_BY_USER_REASON, getCurrentUserId(), GetVietnamTime());

            _signingBatchRepository.Update(signingBatch);
            AddAuditLog(AuditLogConstants.ACTION_SIGNING_BATCH_PAUSE, AuditLogConstants.ACTION_TYPE_UPDATE, $"Tạm dừng lô ký {signingBatch.SignedFolderName}",
                null, new { signingBatch.Id, signingBatch.SignedFiles, signingBatch.FailedFiles });
            await _unitOfWork.SaveChangesAsync();

            _signingRunner.Stop(id);
            return await FindById(id);
        }

        public async Task<FindSigningBatchByIdDto> Cancel(int id)
        {
            _logger.LogInformation($"{nameof(Cancel)}, id={id}");
            var signingBatch = await FindOwnedSigningBatchAsync(id);
            var oldData = new { signingBatch.Id, signingBatch.Status };
            signingBatch.Cancel(getCurrentUserId(), GetVietnamTime());

            _signingBatchRepository.Update(signingBatch);
            AddAuditLog(AuditLogConstants.ACTION_SIGNING_BATCH_CANCEL, AuditLogConstants.ACTION_TYPE_UPDATE, $"Hủy lô ký {signingBatch.SignedFolderName}",
                oldData, new { signingBatch.Id, signingBatch.Status, signingBatch.SignedFiles, signingBatch.FailedFiles });
            await _unitOfWork.SaveChangesAsync();

            _signingRunner.Stop(id);
            _signingQueue.CloseSession(id);
            return await FindById(id);
        }

        public async Task<SigningBatchProgressDto> GetProgress(int id)
        {
            return _signingRunner.GetProgress(id, getCurrentUserId())
                ?? _mapper.Map<SigningBatchProgressDto>(await FindOwnedSigningBatchAsync(id));
        }

        public async Task<List<SignatureRequestDto>> TakeRequests(int id, CancellationToken cancellationToken)
        {
            await FindOwnedSigningBatchAsync(id);
            return await _signingQueue.TakeRequestsAsync(id, cancellationToken);
        }

        public async Task<List<SignatureRequestDto>> SubmitSignatures(int id, List<SignatureResultDto> results, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(SubmitSignatures)}, id={id}, resultCount={results.Count}");
            await FindOwnedSigningBatchAsync(id);
            _signingQueue.SubmitSignatures(id, results);
            return await _signingQueue.TakeRequestsAsync(id, cancellationToken);
        }

        public async Task<string> GetSignedFileUrl(int id, int fileId)
        {
            _logger.LogInformation($"{nameof(GetSignedFileUrl)}, id={id}, fileId={fileId}");
            await FindOwnedSigningBatchAsync(id);
            var file = await _signingBatchRepository.FindFileAsync(id, fileId)
                ?? throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningBatchFileNotFound);
            return _fileStorage.GetTemporaryUrl(file.GetSignedObjectKey(), file.FileName, TimeSpan.FromMinutes(SigningBatchConst.SIGNED_URL_MINUTES));
        }

        private async Task<FindSigningBatchByIdDto> FindById(int id)
        {
            return await _signingBatchQuery.FindById(id)
                ?? throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningBatchNotFound);
        }

        private async Task<SigningBatch> FindOwnedSigningBatchAsync(int id)
        {
            var signingBatch = await _signingBatchRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningBatchNotFound);
            signingBatch.EnsureOwnedBy(getCurrentUserId());
            return signingBatch;
        }
    }
}
