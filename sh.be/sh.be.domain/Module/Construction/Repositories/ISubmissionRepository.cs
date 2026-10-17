using sh.be.domain.Module.Construction.Entities;
using sh.be.domain.Module.Construction.Services.Import.Models;

namespace sh.be.domain.Module.Construction.Repositories
{
    public interface ISubmissionRepository
    {
        Task LockImportAsync(int projectId, int documentTypeId);
        Task<bool> ExistsByFileUrlAsync(string fileUrl);
        Task<List<ExistingSubmissionSnapshot>> GetSnapshotsAsync(int projectId, int documentTypeId);
        Task<List<SubmissionData>> GetDatasAsync(int projectId, int documentTypeId, IReadOnlyCollection<int> documentFieldIds);
        Task BulkInsertSubmissionsAsync(IList<Submission> submissions);
        Task BulkInsertDatasAsync(IList<SubmissionData> datas);
        Task BulkUpdateDatasAsync(IList<SubmissionData> datas);
    }
}
