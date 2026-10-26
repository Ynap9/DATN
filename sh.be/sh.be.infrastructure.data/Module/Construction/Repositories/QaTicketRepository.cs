using EFCore.BulkExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Construction.QaAssignments.Entities;
using sh.be.domain.Module.Construction.QaAssignments.Models;
using sh.be.domain.Module.Construction.QaAssignments.Repositories;
using sh.be.domain.Module.Construction.Submissions.Entities;
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

        public void Add(QaTicket ticket)
        {
            _shDbContext.QaTickets.Add(ticket);
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
    }
}
