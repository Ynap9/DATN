using sh.be.domain.Module.Construction.Projects.Entities;
using sh.be.shared.Constants.Construction;

namespace sh.be.domain.Module.Construction.Projects.Services.Deadline
{
    public static class ProjectDeadlinePolicy
    {
        public static int CalculateDaysLeft(DateTime endedAt, DateTime today)
        {
            return (endedAt.Date - today.Date).Days;
        }

        public static int? Classify(Project project, DateTime today, int warningDays)
        {
            if (!project.EndedAt.HasValue)
            {
                return null;
            }

            var daysLeft = CalculateDaysLeft(project.EndedAt.Value, today);
            if (project.Status == ProjectConst.EXPIRED || daysLeft < 0)
            {
                return ProjectDeadlineConst.NOTIFICATION_EXPIRED;
            }
            return daysLeft <= warningDays ? ProjectDeadlineConst.NOTIFICATION_WARNING : null;
        }

        public static ProjectDeadlineSettings ResolveSettings(IReadOnlyDictionary<string, string> configValues)
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

        public static string BuildSentKey(int projectId, int notificationType, DateTime endedAt, string toAddress)
        {
            return $"{projectId}|{notificationType}|{endedAt:yyyyMMdd}|{toAddress.Trim().ToLowerInvariant()}";
        }
    }
}
