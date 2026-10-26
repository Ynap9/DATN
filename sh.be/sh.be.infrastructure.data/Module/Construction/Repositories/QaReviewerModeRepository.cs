using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Construction.QaAssignments.Entities;
using sh.be.domain.Module.Construction.QaAssignments.Repositories;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Module.Construction.Repositories
{
    public class QaReviewerModeRepository : IQaReviewerModeRepository
    {
        private readonly ShDbContext _shDbContext;

        public QaReviewerModeRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public Task<QaReviewerMode?> FindAsync(int projectId, string userId)
        {
            return _shDbContext.QaReviewerModes.FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId);
        }

        public void Add(QaReviewerMode reviewerMode)
        {
            _shDbContext.QaReviewerModes.Add(reviewerMode);
        }

        public void Update(QaReviewerMode reviewerMode)
        {
            _shDbContext.QaReviewerModes.Update(reviewerMode);
        }
    }
}
