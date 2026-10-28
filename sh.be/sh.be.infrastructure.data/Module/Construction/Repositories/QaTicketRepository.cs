using EFCore.BulkExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Construction.QaAssignments.Entities;
using sh.be.domain.Module.Construction.QaAssignments.Models;
using sh.be.domain.Module.Construction.QaAssignments.Repositories;
using sh.be.domain.Module.Construction.Submissions.Entities;
using sh.be.domain.Module.Operation.ProjectMembers.Entities;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.infrastructure.data.Module.Construction.Repositories
{
    public class QaTicketRepository : IQaTicketRepository
    {
        private const int UniqueIndexViolationErrorNumber = 2601;
        private const int UniqueConstraintViolationErrorNumber = 2627;

        private readonly ShDbContext _shDbContext;

        public QaTicketRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public Task<QaTicket?> FindByIdAsync(int id)
        {
            return _shDbContext.QaTickets.FirstOrDefaultAsync(t => t.Id == id);
        }

        public Task<QaTicket?> FindBySubmissionAsync(int submissionId)
        {
            return _shDbContext.QaTickets
                .FirstOrDefaultAsync(t => _shDbContext.SubmissionUsers.Any(u => u.SubmissionId == submissionId && u.Role == RoleConstants.ROLE_QA
                    && u.TicketId == t.Id));
        }

        public void Add(QaTicket ticket)
        {
            _shDbContext.QaTickets.Add(ticket);
        }

        public Task<int> CountNotPassedAsync(int ticketId)
        {
            return _shDbContext.Submissions
                .CountAsync(s => s.StatusQa != SubmissionConst.QA_PASSED
                    && _shDbContext.SubmissionUsers.Any(u => u.SubmissionId == s.Id && u.Role == RoleConstants.ROLE_QA && u.TicketId == ticketId));
        }

        public Task<int> TryConcludeAsync(int ticketId, string newStatus)
        {
            return _shDbContext.QaTickets
                .Where(t => t.Id == ticketId && t.Status == QaAssignmentConst.TICKET_STATUS_ACTIVE)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, newStatus));
        }

        public Task<int> TryCancelAsync(int ticketId)
        {
            return _shDbContext.QaTickets
                .Where(t => t.Id == ticketId
                    && (t.Status == QaAssignmentConst.TICKET_STATUS_ACTIVE || t.Status == QaAssignmentConst.TICKET_STATUS_REJECTED))
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, QaAssignmentConst.TICKET_STATUS_CANCELLED));
        }

        public Task<int> ReleaseNotPassedAsync(int ticketId)
        {
            return _shDbContext.SubmissionUsers
                .Where(u => u.TicketId == ticketId && u.Role == RoleConstants.ROLE_QA
                    && _shDbContext.Submissions.Any(s => s.Id == u.SubmissionId && s.StatusQa != SubmissionConst.QA_PASSED))
                .ExecuteDeleteAsync();
        }

        public Task<List<int>> GetIdsInOpenTicketAsync(IReadOnlyCollection<int> submissionIds)
        {
            return _shDbContext.SubmissionUsers
                .AsNoTracking()
                .Where(u => submissionIds.Contains(u.SubmissionId) && u.Role == RoleConstants.ROLE_QA
                    && _shDbContext.QaTickets.Any(t => t.Id == u.TicketId
                        && (t.Status == QaAssignmentConst.TICKET_STATUS_ACTIVE || t.Status == QaAssignmentConst.TICKET_STATUS_REJECTED)))
                .Select(u => u.SubmissionId)
                .ToListAsync();
        }

        public Task AddQaAssignmentsAsync(IList<SubmissionUser> qaAssignments)
        {
            try
            {
                _shDbContext.BulkInsert(qaAssignments, new BulkConfig
                {
                    BatchSize = SubmissionConst.BULK_BATCH_SIZE,
                    BulkCopyTimeout = SubmissionConst.BULK_TIMEOUT_SECONDS,
                });
                return Task.CompletedTask;
            }
            catch (SqlException ex) when (ex.Number is UniqueIndexViolationErrorNumber or UniqueConstraintViolationErrorNumber)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorQaAssignConflict);
            }
        }

        public Task<List<QaCandidate>> GetCandidatesAsync(int projectId, int documentTypeId)
        {
            return _shDbContext.Submissions
                .AsNoTracking()
                .Where(s => s.ProjectId == projectId && s.DocumentTypeId == documentTypeId
                    && s.StatusCheck == SubmissionConst.CHECKED && s.StatusQa == SubmissionConst.QA_NOT_REVIEWED
                    && !_shDbContext.SubmissionUsers.Any(u => u.SubmissionId == s.Id && u.Role == RoleConstants.ROLE_QA))
                .OrderBy(s => s.Box)
                .ThenBy(s => s.ProfileRange)
                .ThenBy(s => s.Id)
                .Select(s => new QaCandidate(s.Id, s.Box, s.ProfileRange))
                .ToListAsync();
        }

        public async Task<bool> AnyEnteredByAsync(IReadOnlyCollection<int> submissionIds, string userId)
        {
            foreach (var submissionIdChunk in submissionIds.Chunk(QaAssignmentConst.UPDATE_CHUNK_SIZE))
            {
                var isEnteredBy = await _shDbContext.Submissions
                    .AsNoTracking()
                    .Where(s => submissionIdChunk.Contains(s.Id))
                    .AnyAsync(s => _shDbContext.SubmissionLogs
                        .Where(l => l.SubmissionId == s.Id)
                        .OrderByDescending(l => l.CreatedDate)
                        .ThenByDescending(l => l.Id)
                        .Select(l => l.UserId)
                        .FirstOrDefault() == userId);
                if (isEnteredBy)
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<List<(int ProjectId, int DocumentTypeId)>> GetLeaderPairsAsync(string userId)
        {
            var pairs = await _shDbContext.ProjectMembers
                .AsNoTracking()
                .Where(m => m.UserId == userId && m.Role == RoleConstants.ROLE_LEADER && m.IsActive)
                .Select(m => new { m.ProjectId, m.DocumentTypeId })
                .ToListAsync();
            return pairs.Select(pair => (pair.ProjectId, pair.DocumentTypeId)).ToList();
        }

        public Task<ProjectMember?> FindActiveQaMemberAsync(int projectId, int documentTypeId, string userId)
        {
            return _shDbContext.ProjectMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.DocumentTypeId == documentTypeId && m.UserId == userId
                    && m.Role == RoleConstants.ROLE_QA && m.IsActive);
        }
    }
}
