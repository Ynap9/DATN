using EFCore.BulkExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Construction.QaAssignments.Entities;
using sh.be.domain.Module.Construction.Submissions.Entities;
using sh.be.domain.Module.Operation.Assignments.Entities;
using sh.be.domain.Module.Operation.Assignments.Repositories;
using sh.be.domain.Module.Operation.Assignments.Services;
using sh.be.domain.Module.Operation.ProjectMembers.Entities;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Operation;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.infrastructure.data.Module.Operation.Repositories
{
    public class AssignmentRepository : IAssignmentRepository
    {
        private const int LockTimeoutErrorNumber = 1222;
        private const int UniqueIndexViolationErrorNumber = 2601;
        private const int UniqueConstraintViolationErrorNumber = 2627;

        private static readonly string LockPairSql =
            $"SET LOCK_TIMEOUT {SubmissionConst.IMPORT_LOCK_TIMEOUT_MILLISECONDS}; "
            + "SELECT ProjectId FROM [construction].[ProjectDocumentType] WITH (UPDLOCK, HOLDLOCK) WHERE ProjectId = {0} AND DocumentTypeId = {1}; "
            + "SET LOCK_TIMEOUT -1;";

        private readonly ShDbContext _shDbContext;

        public AssignmentRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public async Task LockPairAsync(int projectId, int documentTypeId)
        {
            try
            {
                await _shDbContext.Database.ExecuteSqlRawAsync(LockPairSql, projectId, documentTypeId);
            }
            catch (SqlException ex) when (ex.Number == LockTimeoutErrorNumber)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorAssignmentConflict);
            }
        }

        public Task<ProjectMember?> FindActiveMemberAsync(int projectId, int documentTypeId, string userId, string role)
        {
            return _shDbContext.ProjectMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.DocumentTypeId == documentTypeId && m.UserId == userId
                    && m.Role == role && m.IsActive);
        }

        public Task<List<AssignmentSubmissionRow>> GetSubmissionRowsAsync(int projectId, int documentTypeId, List<string> boxes)
        {
            return (from s in _shDbContext.Submissions.AsNoTracking()
                    where s.ProjectId == projectId && s.DocumentTypeId == documentTypeId && boxes.Contains(s.Box)
                    join dataHolder in _shDbContext.SubmissionUsers.Where(u => u.Role == RoleConstants.ROLE_DATA) on s.Id equals dataHolder.SubmissionId into dataHolders
                    from dataHolder in dataHolders.DefaultIfEmpty()
                    join qaHolder in _shDbContext.SubmissionUsers.Where(u => u.Role == RoleConstants.ROLE_QA) on s.Id equals qaHolder.SubmissionId into qaHolders
                    from qaHolder in qaHolders.DefaultIfEmpty()
                    select new AssignmentSubmissionRow(s.Id, s.Box, s.ProfileRange, s.StatusCheck, s.StatusQa,
                        dataHolder == null ? null : dataHolder.UserId, qaHolder == null ? null : qaHolder.UserId))
                .ToListAsync();
        }

        public async Task<HashSet<int>> GetSubmissionIdsCheckedByAsync(List<int> submissionIds, string userId)
        {
            HashSet<int> checkedIds = [];
            foreach (var submissionIdChunk in submissionIds.Chunk(AssignmentConst.ID_CHUNK_SIZE))
            {
                checkedIds.UnionWith(await _shDbContext.SubmissionLogs
                    .AsNoTracking()
                    .Where(l => submissionIdChunk.Contains(l.SubmissionId) && l.UserId == userId)
                    .Select(l => l.SubmissionId)
                    .Distinct()
                    .ToListAsync());
            }
            return checkedIds;
        }

        public Task<List<QaOwnership>> GetActiveQaOwnershipsAsync(int projectId, int documentTypeId)
        {
            return (from t in _shDbContext.AssignmentTickets.AsNoTracking()
                    where t.ProjectId == projectId && t.DocumentTypeId == documentTypeId && t.Role == RoleConstants.ROLE_QA
                        && t.Status == AssignmentConst.STATUS_ACTIVE
                    join i in _shDbContext.AssignmentTicketItems on t.Id equals i.AssignmentTicketId
                    select new QaOwnership(t.Id, t.AssigneeUserId, i.Box, i.ProfileRange ?? ""))
                .ToListAsync();
        }

        public Task<string?> FindQaReviewerModeAsync(int projectId, string userId)
        {
            return _shDbContext.QaReviewerModes
                .Where(m => m.ProjectId == projectId && m.UserId == userId)
                .Select(m => m.Mode)
                .FirstOrDefaultAsync();
        }

        public Task<AssignmentTicket?> FindTicketByIdAsync(int id)
        {
            return _shDbContext.AssignmentTickets.FirstOrDefaultAsync(t => t.Id == id);
        }

        public Task<QaTicket?> FindOpenQaBatchAsync(int projectId, int documentTypeId, string qaUserId, int addedCount)
        {
            return _shDbContext.QaTickets
                .Where(t => t.ProjectId == projectId && t.DocumentTypeId == documentTypeId && t.QaUserId == qaUserId
                    && t.Mode == QaAssignmentConst.MODE_FULL && t.Status == QaAssignmentConst.TICKET_STATUS_ACTIVE
                    && t.DocumentCount + addedCount <= QaAssignmentConst.TICKET_MAX_DOCUMENTS
                    && !_shDbContext.SubmissionUsers.Any(u => u.TicketId == t.Id && u.Role == RoleConstants.ROLE_QA
                        && _shDbContext.SubmissionQaLogs.Any(l => l.SubmissionId == u.SubmissionId && l.CreatedDate >= u.CreatedDate)))
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();
        }

        public void AddTicket(AssignmentTicket ticket)
        {
            _shDbContext.AssignmentTickets.Add(ticket);
        }

        public void AddTicketItems(List<AssignmentTicketItem> items)
        {
            _shDbContext.AssignmentTicketItems.AddRange(items);
        }

        public void AddQaTicket(QaTicket qaTicket)
        {
            _shDbContext.QaTickets.Add(qaTicket);
        }

        public Task AddHoldersAsync(List<SubmissionUser> holders)
        {
            try
            {
                _shDbContext.BulkInsert(holders, new BulkConfig
                {
                    BatchSize = SubmissionConst.BULK_BATCH_SIZE,
                    BulkCopyTimeout = SubmissionConst.BULK_TIMEOUT_SECONDS,
                });
                return Task.CompletedTask;
            }
            catch (SqlException ex) when (ex.Number is UniqueIndexViolationErrorNumber or UniqueConstraintViolationErrorNumber)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorAssignmentConflict);
            }
        }

        // Dòng người giữ văn bản theo vai trò (tracked), null nếu chưa giao
        public Task<SubmissionUser?> FindHolderAsync(int submissionId, string role)
        {
            return _shDbContext.SubmissionUsers.FirstOrDefaultAsync(u => u.SubmissionId == submissionId && u.Role == role);
        }

        // Xoá cứng dòng người giữ, văn bản về pool
        public void DeleteHolder(SubmissionUser holder)
        {
            _shDbContext.SubmissionUsers.Remove(holder);
        }

        // Còn là thành viên chưa gỡ của cặp, không xét bật / tắt
        public Task<bool> IsMemberAsync(int projectId, int documentTypeId, string userId)
        {
            return _shDbContext.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.DocumentTypeId == documentTypeId && m.UserId == userId);
        }

        // Đếm văn bản của phiếu nhập liệu chưa QA đạt
        public Task<int> CountDataNotPassedAsync(int ticketId)
        {
            return _shDbContext.SubmissionUsers
                .CountAsync(u => u.TicketId == ticketId && u.Role == RoleConstants.ROLE_DATA
                    && _shDbContext.Submissions.Any(s => s.Id == u.SubmissionId && s.StatusQa != SubmissionConst.QA_PASSED));
        }

        // Chốt phiếu DONE khi phiếu còn ACTIVE, trả số dòng đã đổi
        public Task<int> TryMarkDoneAsync(int ticketId, DateTime doneAt)
        {
            return _shDbContext.AssignmentTickets
                .Where(t => t.Id == ticketId && t.Status == AssignmentConst.STATUS_ACTIVE)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.Status, AssignmentConst.STATUS_DONE)
                    .SetProperty(t => t.DoneAt, doneAt)
                    .SetProperty(t => t.ModifiedDate, doneAt));
        }
    }
}
