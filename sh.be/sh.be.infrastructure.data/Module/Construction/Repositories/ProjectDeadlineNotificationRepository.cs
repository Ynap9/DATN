using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Construction.Projects.Entities;
using sh.be.domain.Module.Construction.Projects.Repositories;
using sh.be.domain.Module.Construction.Projects.Services.Deadline;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Module.Construction.Repositories
{
    public class ProjectDeadlineNotificationRepository : IProjectDeadlineNotificationRepository
    {
        private readonly ShDbContext _shDbContext;

        public ProjectDeadlineNotificationRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public async Task<HashSet<string>> GetSentKeysAsync(IReadOnlyCollection<int> projectIds)
        {
            var sentNotifications = await _shDbContext.ProjectDeadlineNotifications
                .AsNoTracking()
                .Where(n => n.IsSuccess && projectIds.Contains(n.ProjectId))
                .Select(n => new { n.ProjectId, n.NotificationType, n.EndedAt, n.ToAddress })
                .ToListAsync();
            return sentNotifications
                .Select(n => ProjectDeadlinePolicy.BuildSentKey(n.ProjectId, n.NotificationType, n.EndedAt, n.ToAddress))
                .ToHashSet();
        }

        public void Add(ProjectDeadlineNotification notification)
        {
            _shDbContext.ProjectDeadlineNotifications.Add(notification);
        }
    }
}
