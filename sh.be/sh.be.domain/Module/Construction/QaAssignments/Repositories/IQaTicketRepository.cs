using sh.be.domain.Module.Construction.QaAssignments.Entities;
using sh.be.domain.Module.Construction.QaAssignments.Models;
using sh.be.domain.Module.Construction.Submissions.Entities;

namespace sh.be.domain.Module.Construction.QaAssignments.Repositories
{
    public interface IQaTicketRepository
    {
        Task<QaTicket?> FindByIdAsync(int id);
        Task<QaTicket?> FindBySubmissionAsync(int submissionId);
        void Add(QaTicket ticket);
        Task<int> CountNotPassedAsync(int ticketId);
        Task<int> TryConcludeAsync(int ticketId, string newStatus);
        Task<int> TryCancelAsync(int ticketId);
        Task<int> ReleaseNotPassedAsync(int ticketId);
        Task<List<int>> GetIdsInOpenTicketAsync(IReadOnlyCollection<int> submissionIds);
        Task AddQaAssignmentsAsync(IList<SubmissionUser> qaAssignments);
        Task<List<QaCandidate>> GetCandidatesAsync(int projectId, int documentTypeId);
        Task<bool> AnyEnteredByAsync(IReadOnlyCollection<int> submissionIds, string userId);
        Task<List<(int ProjectId, int DocumentTypeId)>> GetLeaderPairsAsync(string userId);
    }
}
