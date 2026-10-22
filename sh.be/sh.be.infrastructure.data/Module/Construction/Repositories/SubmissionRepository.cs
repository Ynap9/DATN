using EFCore.BulkExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Construction.Entities;
using sh.be.domain.Module.Construction.Repositories;
using sh.be.domain.Module.Construction.Services.Import.Models;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.infrastructure.data.Module.Construction.Repositories
{
    public class SubmissionRepository : ISubmissionRepository
    {
        private const int LockTimeoutErrorNumber = 1222;
        private const int UniqueIndexViolationErrorNumber = 2601;
        private const int UniqueConstraintViolationErrorNumber = 2627;

        private static readonly string LockImportSql =
            $"SET LOCK_TIMEOUT {SubmissionConst.IMPORT_LOCK_TIMEOUT_MILLISECONDS}; "
            + "SELECT ProjectId FROM [construction].[ProjectDocumentType] WITH (UPDLOCK, HOLDLOCK) WHERE ProjectId = {0} AND DocumentTypeId = {1}; "
            + "SET LOCK_TIMEOUT -1;";

        private readonly ShDbContext _shDbContext;

        public SubmissionRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public async Task LockImportAsync(int projectId, int documentTypeId)
        {
            try
            {
                await _shDbContext.Database.ExecuteSqlRawAsync(LockImportSql, projectId, documentTypeId);
            }
            catch (SqlException ex) when (ex.Number == LockTimeoutErrorNumber)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorImportInProgress);
            }
        }

        public Task<Submission?> FindByIdAsync(int id)
        {
            return _shDbContext.Submissions.FirstOrDefaultAsync(s => s.Id == id);
        }

        public Task<List<Submission>> GetByIdsAsync(IReadOnlyCollection<int> ids)
        {
            return _shDbContext.Submissions
                .Where(s => ids.Contains(s.Id))
                .ToListAsync();
        }

        public Task<int> SoftDeleteDatasBySubmissionIdsAsync(IReadOnlyCollection<int> submissionIds, string? deletedBy, DateTime deletedDate)
        {
            return _shDbContext.SubmissionDatas
                .Where(sd => submissionIds.Contains(sd.SubmissionId))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(sd => sd.Deleted, true)
                    .SetProperty(sd => sd.DeletedBy, deletedBy)
                    .SetProperty(sd => sd.DeletedDate, deletedDate));
        }

        public Task<List<SubmissionData>> GetDatasBySubmissionIdAsync(int submissionId)
        {
            return _shDbContext.SubmissionDatas
                .Where(sd => sd.SubmissionId == submissionId)
                .ToListAsync();
        }

        public void AddDatas(IEnumerable<SubmissionData> datas)
        {
            _shDbContext.SubmissionDatas.AddRange(datas);
        }

        public void AddLog(SubmissionLog log)
        {
            _shDbContext.SubmissionLogs.Add(log);
        }

        public void AddQaLog(SubmissionQaLog log)
        {
            _shDbContext.SubmissionQaLogs.Add(log);
        }

        public void AddQaLogFields(IEnumerable<SubmissionQaLogField> fields)
        {
            _shDbContext.SubmissionQaLogFields.AddRange(fields);
        }

        public Task<bool> ExistsByFileUrlAsync(string fileUrl)
        {
            return _shDbContext.Submissions.AnyAsync(s => s.FileUrlS3Object == fileUrl);
        }

        public Task<List<ExistingSubmissionSnapshot>> GetSnapshotsAsync(int projectId, int documentTypeId)
        {
            return _shDbContext.Submissions
                .AsNoTracking()
                .Where(s => s.ProjectId == projectId && s.DocumentTypeId == documentTypeId)
                .Select(s => new ExistingSubmissionSnapshot(s.Id, s.FileName, s.ProfileRange, s.Box, s.StatusCheck))
                .ToListAsync();
        }

        public Task<List<SubmissionData>> GetDatasAsync(int projectId, int documentTypeId, IReadOnlyCollection<int> documentFieldIds)
        {
            return Task.FromResult(_shDbContext.SubmissionDatas
                .AsNoTracking()
                .Where(d => d.ProjectId == projectId && d.DocumentTypeId == documentTypeId && documentFieldIds.Contains(d.DocumentFieldId))
                .ToList());
        }

        public Task BulkInsertSubmissionsAsync(IList<Submission> submissions)
        {
            var bulkConfig = CreateBulkConfig();
            bulkConfig.SetOutputIdentity = true;
            return ExecuteBulk(() => _shDbContext.BulkInsert(submissions, bulkConfig));
        }

        public Task BulkInsertDatasAsync(IList<SubmissionData> datas)
        {
            return ExecuteBulk(() => _shDbContext.BulkInsert(datas, CreateBulkConfig()));
        }

        public Task BulkUpdateDatasAsync(IList<SubmissionData> datas)
        {
            var bulkConfig = CreateBulkConfig();
            bulkConfig.PropertiesToInclude =
            [
                nameof(SubmissionData.Id),
                nameof(SubmissionData.Data),
                nameof(SubmissionData.IndexGoogleSheet),
                nameof(SubmissionData.ModifiedBy),
                nameof(SubmissionData.ModifiedDate),
            ];
            return ExecuteBulk(() => _shDbContext.BulkUpdate(datas, bulkConfig));
        }

        private static BulkConfig CreateBulkConfig()
        {
            return new BulkConfig
            {
                BatchSize = SubmissionConst.BULK_BATCH_SIZE,
                BulkCopyTimeout = SubmissionConst.BULK_TIMEOUT_SECONDS,
            };
        }

        private static Task ExecuteBulk(Action bulkOperation)
        {
            try
            {
                bulkOperation();
                return Task.CompletedTask;
            }
            catch (SqlException ex) when (ex.Number is UniqueIndexViolationErrorNumber or UniqueConstraintViolationErrorNumber)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorImportInProgress);
            }
        }
    }
}
