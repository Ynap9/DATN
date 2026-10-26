using sh.be.domain.Module.Construction.QaAssignments.Entities;

namespace sh.be.domain.Module.Construction.QaAssignments.Repositories
{
    public interface IQaReviewerModeRepository
    {
        Task<QaReviewerMode?> FindAsync(int projectId, string userId);
        void Add(QaReviewerMode reviewerMode);
        void Update(QaReviewerMode reviewerMode);
    }
}
