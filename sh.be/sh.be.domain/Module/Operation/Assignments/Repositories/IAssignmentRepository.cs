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
        Task<SubmissionUser?> FindHolderAsync(int submissionId, string role);
        void DeleteHolder(SubmissionUser holder);
        Task<bool> IsMemberAsync(int projectId, int documentTypeId, string userId);
        Task<int> CountDataNotDoneAsync(int ticketId, bool isQaEnabled);
        Task<int> TryMarkDoneAsync(int ticketId, DateTime doneAt);
        Task<List<AssignmentHolderRow>> GetHolderRowsAsync(int projectId, int documentTypeId, string role, string userId, List<AssignmentItem> items);
        Task<int> DeleteUnfinishedHoldersAsync(string role, string userId, List<int> submissionIds);
        Task<List<AssignmentTicketItem>> GetTicketItemsAsync(int ticketId);
        void RemoveTicketItems(List<AssignmentTicketItem> items);
        Task<List<QaTicket>> FindQaBatchesAsync(List<int> qaTicketIds);
        Task<int> CountNotPassedInQaBatchAsync(int qaTicketId);
        Task<int> DecreaseQaBatchDocumentsAsync(int qaTicketId, int count, string? modifiedBy, DateTime modifiedDate);
        Task<int> DecreaseOwnershipDocumentsAsync(int ticketId, int count, string? modifiedBy, DateTime modifiedDate);
    }
}
