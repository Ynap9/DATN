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

        // DB đọc snapshot (RCSI): QA song song 2 văn bản cuối sẽ cùng thấy văn bản kia chưa đạt nên phải khoá dự án trước khi đếm
        // để request sau chờ request trước commit; UPDATE có điều kiện để chỉ 1 request chuyển
        public async Task<bool> TryStartAcceptanceAsync(Project project, string? modifiedBy, DateTime modifiedDate)
        {
            if (!ProjectConst.ACCEPTANCE_START_STATUSES.Contains(project.Status))
            {
                return false;
            }
            await _projectRepository.LockAsync(project.Id);
            if (!(await _projectRepository.CountAcceptanceBlockersAsync(project.Id, project.IsQaEnabled)).IsReady)
            {
                return false;
            }
            if (await _projectRepository.TryStartAcceptanceAsync(project.Id) == 0)
            {
                return false;
            }
            project.StartAcceptance(modifiedBy, modifiedDate);
            return true;
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

        // Admin / SuperAdmin thao tác mọi dự án; người khác chỉ dự án mình là Leader đang hoạt động (ít nhất một loại văn bản)
        public async Task EnsureCanAcceptAsync(int projectId, string userId, bool isAdmin)
        {
            if (!isAdmin && !await _projectRepository.IsActiveLeaderAsync(projectId, userId))
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotManaged);
            }
        }

        public static void EnsureCanReopen(bool isAdmin)
        {
            if (!isAdmin)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Chỉ Admin được trả dự án về đang làm");
            }
        }

        // Dự án chưa có văn bản thì chặn hẳn, kể cả Admin; đủ điều kiện thì bỏ qua cờ isForced; chưa đủ thì chỉ Admin được cho phép.
        // Lý do luôn tuỳ chọn (bắt đầu và hoàn tất); trả lý do đã trim để ghi audit
        public static string? EnsureAcceptanceAllowed(ProjectAcceptanceBlockers blockers, bool isForced, bool isAdmin, string? reason)
        {
            if (blockers.TotalCount == 0)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotReadyForAcceptance, "Dự án chưa có văn bản nào");
            }
            if (!blockers.IsReady && !isForced)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotReadyForAcceptance, blockers.IsQaEnabled
                    ? $"Còn {blockers.NotCheckedCount} văn bản chưa kiểm, {blockers.NotPassedCount} văn bản chưa QA đạt"
                    : $"Còn {blockers.NotCheckedCount} văn bản chưa kiểm");
            }
            if (!blockers.IsReady && !isAdmin)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Chỉ Admin được cho phép khi chưa đủ điều kiện");
            }
            return NormalizeOptionalAcceptanceReason(reason);
        }

        private static string? NormalizeOptionalAcceptanceReason(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return null;
            }
            var trimmedReason = reason.Trim();
            if (trimmedReason.Length > ProjectConst.ACCEPTANCE_REASON_MAX)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Lý do tối đa {ProjectConst.ACCEPTANCE_REASON_MAX} ký tự");
            }
            return trimmedReason;
        }

        public static string NormalizeAcceptanceReason(string? reason)
        {
            var trimmedReason = reason?.Trim() ?? string.Empty;
            if (trimmedReason.Length < ProjectConst.ACCEPTANCE_REASON_MIN || trimmedReason.Length > ProjectConst.ACCEPTANCE_REASON_MAX)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest,
                    $"Lý do phải từ {ProjectConst.ACCEPTANCE_REASON_MIN} đến {ProjectConst.ACCEPTANCE_REASON_MAX} ký tự");
            }
            return trimmedReason;
        }
    }

    public record ProjectDeadlineSettings(int WarningDays, bool IsMailEnabled, List<string> DefaultedConfigNames);

    // NotChecked = chưa CHECKED (gồm trả lại); NotPassed = đã CHECKED nhưng chưa QA đạt, bỏ qua khi dự án không QA;
    // tính mọi văn bản chưa xoá của dự án
    public record ProjectAcceptanceBlockers(int NotCheckedCount, int NotPassedCount, int TotalCount, bool IsQaEnabled)
    {
        public bool IsReady => TotalCount > 0 && NotCheckedCount == 0 && (!IsQaEnabled || NotPassedCount == 0);
    }
}
