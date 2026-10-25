using sh.be.domain.Module.Construction.Projects.Entities;

namespace sh.be.domain.Module.Construction.Projects.Repositories
{
    public interface IProjectDeadlineNotificationRepository
    {
        Task<HashSet<string>> GetSentKeysAsync(IReadOnlyCollection<int> projectIds);
        void Add(ProjectDeadlineNotification notification);
    }
}
