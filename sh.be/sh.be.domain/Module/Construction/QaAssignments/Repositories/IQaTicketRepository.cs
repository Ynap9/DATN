using sh.be.domain.Module.Construction.QaAssignments.Entities;
using sh.be.domain.Module.Construction.QaAssignments.Models;
using sh.be.domain.Module.Construction.Submissions.Entities;

namespace sh.be.domain.Module.Construction.QaAssignments.Repositories
{
    public interface IQaTicketRepository
    {
        Task<QaTicket?> FindByIdAsync(int id);
        void Add(QaTicket ticket);
        Task AddQaAssignmentsAsync(IList<SubmissionUser> qaAssignments);
        Task<List<QaCandidate>> GetCandidatesAsync(int projectId, int documentTypeId);
        Task<bool> AnyEnteredByAsync(IReadOnlyCollection<int> submissionIds, string userId);
        Task<List<(int ProjectId, int DocumentTypeId)>> GetLeaderPairsAsync(string userId);
    }
}
