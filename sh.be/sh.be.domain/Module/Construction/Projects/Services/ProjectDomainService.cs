using sh.be.domain.Module.Construction.Projects.Entities;
using sh.be.domain.Module.Construction.Projects.Repositories;
using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Construction.Projects.Services
{
    public class ProjectDomainService
    {
        private readonly IProjectRepository _projectRepository;

        public ProjectDomainService(IProjectRepository projectRepository)
        {
            _projectRepository = projectRepository;
        }

        public async Task EnsureUniqueNameAsync(string name, int? excludedId = null)
        {
            if (await _projectRepository.ExistsByNameAsync(name, excludedId))
            {
                throw new UserFriendlyException(ErrorCodes.Found, "Tên dự án đã tồn tại");
            }
        }

        public static int CalculateDeadlineDaysLeft(DateTime endedAt, DateTime today)
        {
            return (endedAt.Date - today.Date).Days;
        }

        public static int? ClassifyDeadline(Project project, DateTime today, int warningDays)
        {
            if (!project.EndedAt.HasValue)
            {
                return null;
            }

            var daysLeft = CalculateDeadlineDaysLeft(project.EndedAt.Value, today);
            if (project.Status == ProjectConst.EXPIRED || daysLeft < 0)
            {
                return ProjectDeadlineConst.NOTIFICATION_EXPIRED;
            }
            return daysLeft <= warningDays ? ProjectDeadlineConst.NOTIFICATION_WARNING : null;
        }

        public static ProjectDeadlineSettings ResolveDeadlineSettings(Dictionary<string, string> configValues)
        {
            var defaultedConfigNames = new List<string>();

            var warningDays = ProjectConst.DEFAULT_DEADLINE_WARNING_DAYS;
            if (configValues.TryGetValue(ProjectConfigConst.DEADLINE_WARNING_DAYS, out var warningDaysValue)
                && int.TryParse(warningDaysValue, out var parsedWarningDays) && parsedWarningDays >= 0)
            {
                warningDays = parsedWarningDays;
            }
            else
            {
                defaultedConfigNames.Add(ProjectConfigConst.DEADLINE_WARNING_DAYS);
            }

            var isMailEnabled = true;
            if (configValues.TryGetValue(ProjectConfigConst.DEADLINE_MAIL_ENABLED, out var mailEnabledValue)
                && bool.TryParse(mailEnabledValue, out var parsedMailEnabled))
            {
                isMailEnabled = parsedMailEnabled;
            }
            else
            {
                defaultedConfigNames.Add(ProjectConfigConst.DEADLINE_MAIL_ENABLED);
            }

            return new ProjectDeadlineSettings(warningDays, isMailEnabled, defaultedConfigNames);
        }

        public static string BuildDeadlineSentKey(int projectId, int notificationType, DateTime endedAt, string toAddress)
        {
            return $"{projectId}|{notificationType}|{endedAt:yyyyMMdd}|{toAddress.Trim().ToLowerInvariant()}";
        }
    }

    public record ProjectDeadlineSettings(int WarningDays, bool IsMailEnabled, List<string> DefaultedConfigNames);
}
