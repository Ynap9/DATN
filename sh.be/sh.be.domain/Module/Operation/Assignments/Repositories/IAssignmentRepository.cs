using sh.be.domain.Module.Construction.QaAssignments.Entities;
using sh.be.domain.Module.Construction.Submissions.Entities;
using sh.be.domain.Module.Operation.Assignments.Entities;
using sh.be.domain.Module.Operation.Assignments.Services;
using sh.be.domain.Module.Operation.ProjectMembers.Entities;

namespace sh.be.domain.Module.Operation.Assignments.Repositories
{
    public interface IAssignmentRepository
    {
        Task LockPairAsync(int projectId, int documentTypeId);
        Task<ProjectMember?> FindActiveMemberAsync(int projectId, int documentTypeId, string userId, string role);
        Task<List<AssignmentSubmissionRow>> GetSubmissionRowsAsync(int projectId, int documentTypeId, List<string> boxes);
        Task<HashSet<int>> GetSubmissionIdsCheckedByAsync(List<int> submissionIds, string userId);
        Task<List<QaOwnership>> GetActiveQaOwnershipsAsync(int projectId, int documentTypeId);
        Task<string?> FindQaReviewerModeAsync(int projectId, string userId);
        Task<AssignmentTicket?> FindTicketByIdAsync(int id);
        Task<QaTicket?> FindOpenQaBatchAsync(int projectId, int documentTypeId, string qaUserId, int addedCount);
        void AddTicket(AssignmentTicket ticket);
        void AddTicketItems(List<AssignmentTicketItem> items);
        void AddQaTicket(QaTicket qaTicket);
        Task AddHoldersAsync(List<SubmissionUser> holders);
    }
}
