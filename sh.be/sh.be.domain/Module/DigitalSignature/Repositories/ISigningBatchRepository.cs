using sh.be.domain.Module.DigitalSignature.Entities;

namespace sh.be.domain.Module.DigitalSignature.Repositories
{
    public interface ISigningBatchRepository
    {
        Task<SigningBatch?> FindByIdAsync(int id);
        void Add(SigningBatch signingBatch);
        void Update(SigningBatch signingBatch);
        Task<SigningBatchFile?> FindFileAsync(int signingBatchId, int fileId);
        Task<List<string>> GetFileNamesAsync(int signingBatchId);
        Task<int> GetLastFileOrderAsync(int signingBatchId);
        void AddFiles(List<SigningBatchFile> files);
        Task<List<SigningBatchFile>> GetFailedFilesAsync(int signingBatchId);
        Task<int> CountPendingFilesAsync(int signingBatchId);
        Task<List<SigningBatchFile>> ClaimFilesAsync(int signingBatchId, int take, string? modifiedBy, DateTime modifiedDate);
        Task MarkFileSignedAsync(SigningBatchFile file, string signedObjectKey, DateTime signedAt, DateTime? timestampedAt, string? modifiedBy, DateTime modifiedDate);
        Task MarkFileFailedAsync(SigningBatchFile file, string errorMessage, string? modifiedBy, DateTime modifiedDate);
        Task ReturnSigningFilesAsync(int signingBatchId, string? modifiedBy, DateTime modifiedDate);
        Task PauseInterruptedAsync(string stopReason, DateTime modifiedDate);
    }
}
