using sh.be.domain.Module.Construction.Entities;
using sh.be.domain.Module.Construction.Services.Import.Models;

namespace sh.be.domain.Module.Construction.Repositories
{
    public interface ISubmissionRepository
    {
        Task<Submission?> FindByIdAsync(int id);
        Task<List<SubmissionData>> GetDatasBySubmissionIdAsync(int submissionId);
        void AddDatas(IEnumerable<SubmissionData> datas);
        void AddLog(SubmissionLog log);
        void AddQaLog(SubmissionQaLog log);
        void AddQaLogFields(IEnumerable<SubmissionQaLogField> fields);
        Task LockImportAsync(int projectId, int documentTypeId);
        Task<bool> ExistsByFileUrlAsync(string fileUrl);
        Task<List<ExistingSubmissionSnapshot>> GetSnapshotsAsync(int projectId, int documentTypeId);
        Task<List<SubmissionData>> GetDatasAsync(int projectId, int documentTypeId, IReadOnlyCollection<int> documentFieldIds);
        Task BulkInsertSubmissionsAsync(IList<Submission> submissions);
        Task BulkInsertDatasAsync(IList<SubmissionData> datas);
        Task BulkUpdateDatasAsync(IList<SubmissionData> datas);
    }
}
