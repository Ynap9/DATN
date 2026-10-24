using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Base.Interfaces.External.Mail;
using sh.be.applications.Module.Auth.Interfaces.Queries;
using sh.be.domain.Base;
using sh.be.domain.Module.Construction.Projects.Entities;
using sh.be.domain.Module.Construction.Projects.Repositories;
using sh.be.domain.Module.Construction.Projects.Services;
using sh.be.domain.Module.Construction.Projects.Services.Deadline;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.BackgroundJob;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Core;
using sh.be.shared.Constants.Mail;

namespace sh.be.applications.Module.Construction.Jobs
{
    public class ProjectDeadlineJob : BaseService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectDeadlineNotificationRepository _projectDeadlineNotificationRepository;
        private readonly ProjectConfigDomainService _projectConfigDomainService;
        private readonly IUserQuery _userQuery;
        private readonly IMailExternal _mailExternal;

        public ProjectDeadlineJob(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<ProjectDeadlineJob> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IProjectRepository projectRepository, IProjectDeadlineNotificationRepository projectDeadlineNotificationRepository, ProjectConfigDomainService projectConfigDomainService,
            IUserQuery userQuery, IMailExternal mailExternal
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _projectRepository = projectRepository;
            _projectDeadlineNotificationRepository = projectDeadlineNotificationRepository;
            _projectConfigDomainService = projectConfigDomainService;
            _userQuery = userQuery;
            _mailExternal = mailExternal;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation($"{nameof(ExecuteAsync)}, start");
            if (!await _projectRepository.TryLockDeadlineCheckAsync())
            {
                _logger.LogWarning($"{nameof(ExecuteAsync)}, đang có lần chạy khác");
                return;
            }

            try
            {
                var now = GetVietnamTime();
                var today = now.Date;
                var settings = await _projectConfigDomainService.GetDeadlineSettingsAsync();
                if (settings.DefaultedConfigNames.Count > 0)
                {
                    _logger.LogWarning($"{nameof(ExecuteAsync)}, thiếu hoặc sai cấu hình {string.Join(", ", settings.DefaultedConfigNames)}, dùng mặc định warningDays={settings.WarningDays}, mailEnabled={settings.IsMailEnabled}");
                }

                var projects = await _projectRepository.GetWithEndedAtByStatusesAsync(ProjectConst.DEADLINE_STATUSES);
                var expiredProjectCount = 0;
                await using (var transaction = await _unitOfWork.BeginTransactionAsync())
                {
                    try
                    {
                        foreach (var project in projects)
                        {
                            var oldStatus = project.Status;
                            if (!project.Expire(today, BackgroundJobConst.SYSTEM_USER, now))
                            {
                                continue;
                            }
                            AddAuditLog(AuditLogConstants.ACTION_PROJECT_EXPIRE, AuditLogConstants.ACTION_TYPE_UPDATE,
                                $"Dự án {project.Name} hết hạn (ngày kết thúc {project.EndedAt:dd/MM/yyyy})",
                                oldData: new { Status = oldStatus },
                                newData: new { project.Status },
                                userId: BackgroundJobConst.SYSTEM_USER);
                            expiredProjectCount++;
                        }
                        await _unitOfWork.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{nameof(ExecuteAsync)}, lỗi chuyển dự án hết hạn");
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                if (!settings.IsMailEnabled)
                {
                    _logger.LogInformation($"{nameof(ExecuteAsync)}, end, expiredProjectCount={expiredProjectCount}, mail tắt");
                    return;
                }

                var notifiedProjects = projects
                    .Select(project => (Project: project, NotificationType: ProjectDeadlinePolicy.Classify(project, today, settings.WarningDays)))
                    .Where(notifiedProject => notifiedProject.NotificationType.HasValue)
                    .ToList();
                if (notifiedProjects.Count == 0)
                {
                    _logger.LogInformation($"{nameof(ExecuteAsync)}, end, expiredProjectCount={expiredProjectCount}, không có dự án cần báo");
                    return;
                }

                var recipients = await _userQuery.GetEmailsByRoleNamesAsync([RoleConstants.ROLE_SUPER_ADMIN, RoleConstants.ROLE_ADMIN]);
                if (recipients.Count == 0)
                {
                    _logger.LogWarning($"{nameof(ExecuteAsync)}, end, expiredProjectCount={expiredProjectCount}, không có người nhận mail");
                    return;
                }

                var sentKeys = await _projectDeadlineNotificationRepository.GetSentKeysAsync(notifiedProjects.Select(notifiedProject => notifiedProject.Project.Id).ToList());
                var sentMailCount = 0;
                var failedMailCount = 0;
                foreach (var (project, notificationType) in notifiedProjects)
                {
                    var endedAt = project.EndedAt!.Value.Date;
                    var daysLeft = ProjectDeadlinePolicy.CalculateDaysLeft(endedAt, today);
                    var isExpired = notificationType == ProjectDeadlineConst.NOTIFICATION_EXPIRED;
                    var subject = string.Format(isExpired ? MailTemplateConstants.SUBJECT_PROJECT_EXPIRED : MailTemplateConstants.SUBJECT_PROJECT_DEADLINE_WARNING, project.Name);
                    foreach (var recipient in recipients)
                    {
                        if (!sentKeys.Add(ProjectDeadlinePolicy.BuildSentKey(project.Id, notificationType!.Value, endedAt, recipient)))
                        {
                            continue;
                        }

                        var notification = ProjectDeadlineNotification.Create(project.Id, project.Name, notificationType.Value, endedAt, daysLeft,
                            recipient, subject, BackgroundJobConst.SYSTEM_USER, GetVietnamTime());
                        try
                        {
                            await _mailExternal.SendProjectDeadlineAsync(recipient, subject, project.Name, endedAt, daysLeft, isExpired);
                            sentMailCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"{nameof(ExecuteAsync)}, gửi mail lỗi, projectId={project.Id}, toAddress={recipient}");
                            notification.MarkFailed(ex.Message);
                            failedMailCount++;
                        }
                        _projectDeadlineNotificationRepository.Add(notification);
                    }
                }
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"{nameof(ExecuteAsync)}, end, expiredProjectCount={expiredProjectCount}, sentMailCount={sentMailCount}, failedMailCount={failedMailCount}");
            }
            finally
            {
                await _projectRepository.ReleaseDeadlineCheckLockAsync();
            }
        }
    }
}
