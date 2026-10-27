using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Construction.DocumentTypes.Entities;
using sh.be.domain.Module.Construction.Projects.Entities;
using sh.be.domain.Module.Construction.QaAssignments.Entities;
using sh.be.domain.Module.Operation.Assignments.Entities;
using sh.be.domain.Module.Operation.ProjectMembers.Entities;
using sh.be.domain.Module.Operation.ProjectMembers.Repositories;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Operation;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.infrastructure.data.Module.Operation.Repositories
{
    public class ProjectMemberRepository : IProjectMemberRepository
    {
        private const int UniqueIndexViolationErrorNumber = 2601;
        private const int UniqueConstraintViolationErrorNumber = 2627;

        private readonly ShDbContext _shDbContext;

        public ProjectMemberRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public Task<Project?> FindProjectAsync(int projectId)
        {
            return _shDbContext.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId);
        }

        public Task<DocumentType?> FindDocumentTypeAsync(int documentTypeId)
        {
            return _shDbContext.DocumentTypes.AsNoTracking().FirstOrDefaultAsync(d => d.Id == documentTypeId);
        }

        public Task<bool> IsDocumentTypeInProjectAsync(int projectId, int documentTypeId)
        {
            return _shDbContext.ProjectDocumentTypes.AnyAsync(p => p.ProjectId == projectId && p.DocumentTypeId == documentTypeId);
        }

        public Task<ProjectMember?> FindByIdAsync(int id)
        {
            return _shDbContext.ProjectMembers.FirstOrDefaultAsync(m => m.Id == id);
        }

        public Task<ProjectMember?> FindActiveLeaderAsync(int projectId, int documentTypeId, string userId)
        {
            return _shDbContext.ProjectMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.DocumentTypeId == documentTypeId && m.UserId == userId
                    && m.Role == RoleConstants.ROLE_LEADER && m.IsActive);
        }

        public async Task<HashSet<string>> GetMemberUserIdsAsync(int projectId, int documentTypeId)
        {
            var userIds = await _shDbContext.ProjectMembers
                .Where(m => m.ProjectId == projectId && m.DocumentTypeId == documentTypeId)
                .Select(m => m.UserId)
                .ToListAsync();
            return userIds.ToHashSet();
        }

        public async Task AddRangeAsync(IEnumerable<ProjectMember> projectMembers)
        {
            await _shDbContext.ProjectMembers.AddRangeAsync(projectMembers);
            try
            {
                await _shDbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: UniqueIndexViolationErrorNumber or UniqueConstraintViolationErrorNumber })
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Có người vừa được thêm bởi thao tác khác, hãy tải lại");
            }
        }

        public Task<List<ProjectMember>> GetManagedMembersAsync(int projectId, int documentTypeId, string leaderUserId)
        {
            return _shDbContext.ProjectMembers
                .Where(m => m.ProjectId == projectId && m.DocumentTypeId == documentTypeId && m.ManagedByUserId == leaderUserId)
                .ToListAsync();
        }

        public Task<List<AssignmentTicket>> GetActiveAssignmentTicketsAsync(int projectId, int documentTypeId, string userId)
        {
            return _shDbContext.AssignmentTickets
                .Where(t => t.ProjectId == projectId && t.DocumentTypeId == documentTypeId && t.AssigneeUserId == userId
                    && t.Status == AssignmentConst.STATUS_ACTIVE)
                .ToListAsync();
        }

        public Task<int> ReleaseEntryNotCheckedAsync(int assignmentTicketId)
        {
            return _shDbContext.SubmissionUsers
                .Where(u => u.TicketId == assignmentTicketId && u.Role == RoleConstants.ROLE_DATA
                    && _shDbContext.Submissions.Any(s => s.Id == u.SubmissionId
                        && (s.StatusCheck == SubmissionConst.NOT_CHECKED || s.StatusCheck == SubmissionConst.RETURNED)))
                .ExecuteDeleteAsync();
        }

        public Task<List<QaTicket>> GetOpenQaTicketsAsync(int projectId, int documentTypeId, string userId)
        {
            return _shDbContext.QaTickets
                .Where(t => t.ProjectId == projectId && t.DocumentTypeId == documentTypeId && t.QaUserId == userId
                    && (t.Status == QaAssignmentConst.TICKET_STATUS_ACTIVE || t.Status == QaAssignmentConst.TICKET_STATUS_REJECTED))
                .ToListAsync();
        }

        public Task<int> TryCancelQaTicketAsync(int qaTicketId)
        {
            return _shDbContext.QaTickets
                .Where(t => t.Id == qaTicketId
                    && (t.Status == QaAssignmentConst.TICKET_STATUS_ACTIVE || t.Status == QaAssignmentConst.TICKET_STATUS_REJECTED))
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, QaAssignmentConst.TICKET_STATUS_CANCELLED));
        }

        public Task<int> ReleaseQaNotPassedAsync(int qaTicketId)
        {
            return _shDbContext.SubmissionUsers
                .Where(u => u.TicketId == qaTicketId && u.Role == RoleConstants.ROLE_QA
                    && _shDbContext.Submissions.Any(s => s.Id == u.SubmissionId && s.StatusQa != SubmissionConst.QA_PASSED))
                .ExecuteDeleteAsync();
        }

        public Task<int> CountKeptDocumentsAsync(string role, IReadOnlyCollection<int> ticketIds)
        {
            return _shDbContext.SubmissionUsers.CountAsync(u => u.Role == role && ticketIds.Contains(u.TicketId));
        }

        public async Task<Dictionary<string, List<string>>> GetUserRoleNamesAsync(IEnumerable<string> userIds)
        {
            var distinctUserIds = userIds.Distinct().ToList();
            var existingUserIds = await _shDbContext.Users
                .Where(u => distinctUserIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();
            var userRoles = await (from ur in _shDbContext.UserRoles
                                   join r in _shDbContext.Roles on ur.RoleId equals r.Id
                                   where distinctUserIds.Contains(ur.UserId)
                                   select new { ur.UserId, r.Name }).Distinct().ToListAsync();
            var roleNamesByUser = userRoles.ToLookup(x => x.UserId, x => x.Name);
            return existingUserIds.ToDictionary(userId => userId, userId => roleNamesByUser[userId].ToList());
        }
    }
}
