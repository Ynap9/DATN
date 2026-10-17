using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.DigitalSignature.Entities;
using sh.be.domain.Module.DigitalSignature.Repositories;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.DigitalSignature;

namespace sh.be.infrastructure.data.Module.DigitalSignature.Repositories
{
    public class SigningBatchRepository : ISigningBatchRepository
    {
        private readonly ShDbContext _shDbContext;

        public SigningBatchRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public Task<SigningBatch?> FindByIdAsync(int id)
        {
            return _shDbContext.SigningBatches.FirstOrDefaultAsync(sb => sb.Id == id);
        }

        public void Add(SigningBatch signingBatch)
        {
            _shDbContext.SigningBatches.Add(signingBatch);
        }

        public void Update(SigningBatch signingBatch)
        {
            _shDbContext.SigningBatches.Update(signingBatch);
        }

        public Task<SigningBatchFile?> FindFileAsync(int signingBatchId, int fileId)
        {
            return _shDbContext.SigningBatchFiles.FirstOrDefaultAsync(sbf => sbf.Id == fileId && sbf.SigningBatchId == signingBatchId);
        }

        public Task<List<string>> GetFileNamesAsync(int signingBatchId)
        {
            return _shDbContext.SigningBatchFiles
                .Where(sbf => sbf.SigningBatchId == signingBatchId)
                .Select(sbf => sbf.FileName)
                .ToListAsync();
        }

        public async Task<int> GetLastFileOrderAsync(int signingBatchId)
        {
            return await _shDbContext.SigningBatchFiles
                .IgnoreQueryFilters()
                .Where(sbf => sbf.SigningBatchId == signingBatchId)
                .MaxAsync(sbf => (int?)sbf.Order) ?? 0;
        }

        public void AddFiles(List<SigningBatchFile> files)
        {
            _shDbContext.SigningBatchFiles.AddRange(files);
        }

        public Task<List<SigningBatchFile>> GetFailedFilesAsync(int signingBatchId)
        {
            return _shDbContext.SigningBatchFiles
                .Where(sbf => sbf.SigningBatchId == signingBatchId && sbf.Status == SigningBatchFileConst.FAILED)
                .ToListAsync();
        }

        public Task<int> CountPendingFilesAsync(int signingBatchId)
        {
            return _shDbContext.SigningBatchFiles
                .CountAsync(sbf => sbf.SigningBatchId == signingBatchId && sbf.Status == SigningBatchFileConst.PENDING);
        }

        public async Task<List<SigningBatchFile>> ClaimFilesAsync(int signingBatchId, int take, string? modifiedBy, DateTime modifiedDate)
        {
            var files = await _shDbContext.SigningBatchFiles
                .AsNoTracking()
                .Where(sbf => sbf.SigningBatchId == signingBatchId && sbf.Status == SigningBatchFileConst.PENDING)
                .OrderBy(sbf => sbf.Order)
                .Take(take)
                .ToListAsync();
            var ids = files.Select(sbf => sbf.Id).ToList();
            await _shDbContext.SigningBatchFiles
                .Where(sbf => ids.Contains(sbf.Id) && sbf.Status == SigningBatchFileConst.PENDING)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(sbf => sbf.Status, SigningBatchFileConst.SIGNING)
                    .SetProperty(sbf => sbf.ModifiedBy, modifiedBy)
                    .SetProperty(sbf => sbf.ModifiedDate, modifiedDate));
            return files;
        }

        public async Task MarkFileSignedAsync(SigningBatchFile file, string signedObjectKey, DateTime signedAt, DateTime? timestampedAt, string? modifiedBy, DateTime modifiedDate)
        {
            await _shDbContext.SigningBatchFiles
                .Where(sbf => sbf.Id == file.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(sbf => sbf.Status, SigningBatchFileConst.DONE)
                    .SetProperty(sbf => sbf.SignedObjectKey, signedObjectKey)
                    .SetProperty(sbf => sbf.SignedAt, signedAt)
                    .SetProperty(sbf => sbf.TimestampedAt, timestampedAt)
                    .SetProperty(sbf => sbf.IsSignatureValid, true)
                    .SetProperty(sbf => sbf.SignatureValidationMessage, (string?)null)
                    .SetProperty(sbf => sbf.ErrorMessage, (string?)null)
                    .SetProperty(sbf => sbf.ModifiedBy, modifiedBy)
                    .SetProperty(sbf => sbf.ModifiedDate, modifiedDate));
            await _shDbContext.SigningBatches
                .Where(sb => sb.Id == file.SigningBatchId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(sb => sb.SignedFiles, sb => sb.SignedFiles + 1)
                    .SetProperty(sb => sb.ModifiedDate, modifiedDate));
        }

        public async Task MarkFileFailedAsync(SigningBatchFile file, string errorMessage, string? modifiedBy, DateTime modifiedDate)
        {
            await _shDbContext.SigningBatchFiles
                .Where(sbf => sbf.Id == file.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(sbf => sbf.Status, SigningBatchFileConst.FAILED)
                    .SetProperty(sbf => sbf.ErrorMessage, errorMessage)
                    .SetProperty(sbf => sbf.ModifiedBy, modifiedBy)
                    .SetProperty(sbf => sbf.ModifiedDate, modifiedDate));
            await _shDbContext.SigningBatches
                .Where(sb => sb.Id == file.SigningBatchId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(sb => sb.FailedFiles, sb => sb.FailedFiles + 1)
                    .SetProperty(sb => sb.ModifiedDate, modifiedDate));
        }

        public Task ReturnSigningFilesAsync(int signingBatchId, string? modifiedBy, DateTime modifiedDate)
        {
            return _shDbContext.SigningBatchFiles
                .Where(sbf => sbf.SigningBatchId == signingBatchId && sbf.Status == SigningBatchFileConst.SIGNING)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(sbf => sbf.Status, SigningBatchFileConst.PENDING)
                    .SetProperty(sbf => sbf.ModifiedBy, modifiedBy)
                    .SetProperty(sbf => sbf.ModifiedDate, modifiedDate));
        }

        public async Task PauseInterruptedAsync(string stopReason, DateTime modifiedDate)
        {
            await _shDbContext.SigningBatchFiles
                .Where(sbf => sbf.Status == SigningBatchFileConst.SIGNING)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(sbf => sbf.Status, SigningBatchFileConst.PENDING)
                    .SetProperty(sbf => sbf.ModifiedDate, modifiedDate));
            await _shDbContext.SigningBatches
                .Where(sb => sb.Status == SigningBatchConst.SIGNING)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(sb => sb.Status, SigningBatchConst.PAUSED)
                    .SetProperty(sb => sb.StopReason, stopReason)
                    .SetProperty(sb => sb.ModifiedDate, modifiedDate));
        }
    }
}
