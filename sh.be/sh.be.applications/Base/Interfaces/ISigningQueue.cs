using sh.be.applications.Base.Dtos.Signing;
using System.Security.Cryptography.X509Certificates;

namespace sh.be.applications.Base.Interfaces
{
    public interface ISigningQueue
    {
        void OpenSession(int signingBatchId, X509Certificate2 certificate);
        void CloseSession(int signingBatchId);
        bool IsSessionAlive(int signingBatchId);
        string? GetSessionThumbprint(int signingBatchId);
        X509Certificate2 CopySessionCertificate(int signingBatchId);
        Task<byte[]> RequestSignatureAsync(int signingBatchId, byte[] data, CancellationToken cancellationToken);
        Task<List<SignatureRequestDto>> TakeRequestsAsync(int signingBatchId, CancellationToken cancellationToken);
        void SubmitSignatures(int signingBatchId, List<SignatureResultDto> results);
    }
}
