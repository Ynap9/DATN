using Microsoft.Extensions.Logging;
using sh.be.applications.Base.Dtos.Signing;
using sh.be.applications.Base.Interfaces;
using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace sh.be.infrastructure.external.Signing.Implements
{
    public class SigningQueue : ISigningQueue
    {
        private readonly ILogger<SigningQueue> _logger;
        private readonly ConcurrentDictionary<int, SigningSession> _sessions = new();

        public SigningQueue(ILogger<SigningQueue> logger)
        {
            _logger = logger;
        }

        public void OpenSession(int signingBatchId, X509Certificate2 certificate)
        {
            _logger.LogInformation($"{nameof(OpenSession)}, signingBatchId={signingBatchId}, thumbprint={certificate.Thumbprint}");
            CloseSession(signingBatchId);
            _sessions[signingBatchId] = new SigningSession(certificate);
        }

        public void CloseSession(int signingBatchId)
        {
            if (_sessions.TryRemove(signingBatchId, out var session))
            {
                _logger.LogInformation($"{nameof(CloseSession)}, signingBatchId={signingBatchId}");
                session.Close();
            }
        }

        public bool IsSessionAlive(int signingBatchId)
        {
            return _sessions.ContainsKey(signingBatchId);
        }

        public string? GetSessionThumbprint(int signingBatchId)
        {
            return _sessions.TryGetValue(signingBatchId, out var session) ? session.Certificate.Thumbprint : null;
        }

        public X509Certificate2 CopySessionCertificate(int signingBatchId)
        {
            return X509CertificateLoader.LoadCertificate(GetSession(signingBatchId).Certificate.RawData);
        }

        public async Task<byte[]> RequestSignatureAsync(int signingBatchId, byte[] data, CancellationToken cancellationToken)
        {
            var session = GetSession(signingBatchId);
            var request = new SignatureRequestDto { RequestId = Guid.NewGuid().ToString("N"), DataBase64 = Convert.ToBase64String(data) };
            var completion = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            session.Waiting[request.RequestId] = completion;
            session.Requests.Enqueue(request);
            session.Available.Release();
            try
            {
                return await completion.Task.WaitAsync(TimeSpan.FromSeconds(SigningBatchConst.SIGNATURE_TIMEOUT_SECONDS), cancellationToken);
            }
            catch (TimeoutException)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningRequestTimeout);
            }
            finally
            {
                session.Waiting.TryRemove(request.RequestId, out _);
            }
        }

        public async Task<List<SignatureRequestDto>> TakeRequestsAsync(int signingBatchId, CancellationToken cancellationToken)
        {
            var session = GetSession(signingBatchId);
            var requests = new List<SignatureRequestDto>();
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.Closed.Token);
            try
            {
                if (!await session.Available.WaitAsync(TimeSpan.FromSeconds(SigningBatchConst.LONG_POLL_SECONDS), cancellation.Token))
                {
                    return requests;
                }
            }
            catch (OperationCanceledException) when (session.Closed.IsCancellationRequested)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningSessionNotOpen);
            }

            do
            {
                if (session.Requests.TryDequeue(out var request) && session.Waiting.ContainsKey(request.RequestId))
                {
                    requests.Add(request);
                }
            }
            while (requests.Count < SigningBatchConst.REQUEST_BATCH_SIZE && session.Available.Wait(0));
            return requests;
        }

        public void SubmitSignatures(int signingBatchId, List<SignatureResultDto> results)
        {
            var session = GetSession(signingBatchId);
            foreach (var result in results.Where(r => !r.IsSessionLost))
            {
                if (!session.Waiting.TryRemove(result.RequestId, out var completion))
                {
                    continue;
                }

                var signature = DecodeSignature(result.SignatureBase64);
                if (signature == null)
                {
                    _logger.LogWarning($"{nameof(SubmitSignatures)}, signingBatchId={signingBatchId}, requestId={result.RequestId}, error={result.Error}");
                    completion.TrySetException(new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSignatureRejected));
                    continue;
                }
                completion.TrySetResult(signature);
            }

            if (results.Any(r => r.IsSessionLost))
            {
                CloseSession(signingBatchId);
            }
        }

        private SigningSession GetSession(int signingBatchId)
        {
            return _sessions.TryGetValue(signingBatchId, out var session)
                ? session
                : throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningSessionNotOpen);
        }

        private static byte[]? DecodeSignature(string? signatureBase64)
        {
            var buffer = new byte[(signatureBase64?.Length ?? 0) * 3 / 4];
            return !string.IsNullOrWhiteSpace(signatureBase64) && Convert.TryFromBase64String(signatureBase64, buffer, out var length) && length > 0
                ? buffer[..length]
                : null;
        }

        private sealed class SigningSession(X509Certificate2 certificate)
        {
            public X509Certificate2 Certificate { get; } = certificate;
            public ConcurrentQueue<SignatureRequestDto> Requests { get; } = new();
            public ConcurrentDictionary<string, TaskCompletionSource<byte[]>> Waiting { get; } = new();
            public SemaphoreSlim Available { get; } = new(0);
            public CancellationTokenSource Closed { get; } = new();

            public void Close()
            {
                Closed.Cancel();
                foreach (var completion in Waiting.Values)
                {
                    completion.TrySetException(new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningSessionNotOpen));
                }
                Certificate.Dispose();
            }
        }
    }
}
