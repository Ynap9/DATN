using sh.be.applications.Module.DigitalSignature.Dtos.SigningBatch;
using sh.be.domain.Module.DigitalSignature.Entities;

namespace sh.be.applications.Module.DigitalSignature.Interfaces.Services
{
    public interface ISigningRunner
    {
        void Start(SigningBatch signingBatch);
        void Stop(int signingBatchId);
        void EnsureNotRunning(int signingBatchId);
        SigningBatchProgressDto? GetProgress(int signingBatchId, string? userId);
        Task RecoverAsync();
    }
}
