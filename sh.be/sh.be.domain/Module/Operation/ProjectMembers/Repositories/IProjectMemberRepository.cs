using sh.be.domain.Module.Construction.DocumentTypes.Entities;
using sh.be.domain.Module.Construction.Projects.Entities;
using sh.be.domain.Module.Construction.QaAssignments.Entities;
using sh.be.domain.Module.Operation.Assignments.Entities;
using sh.be.domain.Module.Operation.ProjectMembers.Entities;

namespace sh.be.domain.Module.Operation.ProjectMembers.Repositories
{
    public interface IProjectMemberRepository
    {
        Task<Project?> FindProjectAsync(int projectId);
        Task<DocumentType?> FindDocumentTypeAsync(int documentTypeId);
        Task<bool> IsDocumentTypeInProjectAsync(int projectId, int documentTypeId);
        Task<ProjectMember?> FindByIdAsync(int id);
        Task<ProjectMember?> FindActiveLeaderAsync(int projectId, int documentTypeId, string userId);
        Task<HashSet<string>> GetMemberUserIdsAsync(int projectId, int documentTypeId);
        Task AddRangeAsync(IEnumerable<ProjectMember> projectMembers);
        Task<List<ProjectMember>> GetManagedMembersAsync(int projectId, int documentTypeId, string leaderUserId);
        Task<List<AssignmentTicket>> GetActiveAssignmentTicketsAsync(int projectId, int documentTypeId, string userId);
        Task<int> ReleaseEntryNotCheckedAsync(int assignmentTicketId);
        Task<List<QaTicket>> GetOpenQaTicketsAsync(int projectId, int documentTypeId, string userId);
        Task<int> TryCancelQaTicketAsync(int qaTicketId);
        Task<int> ReleaseQaNotPassedAsync(int qaTicketId);
        Task<int> CountKeptDocumentsAsync(string role, IReadOnlyCollection<int> ticketIds);
        Task<Dictionary<string, List<string>>> GetUserRoleNamesAsync(IEnumerable<string> userIds);
    }
}
