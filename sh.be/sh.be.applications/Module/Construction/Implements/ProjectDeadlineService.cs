using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Base.Interfaces.External.BackgroundJob;
using sh.be.applications.Module.Auth.Interfaces.Queries;
using sh.be.applications.Module.Construction.Dtos.ProjectDeadline;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Construction.Projects.Services;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Core;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;

namespace sh.be.applications.Module.Construction.Implements
{
    public class ProjectDeadlineService : BaseService, IProjectDeadlineService
    {
        private readonly IProjectDeadlineQuery _projectDeadlineQuery;
        private readonly IUserQuery _userQuery;
        private readonly ProjectConfigDomainService _projectConfigDomainService;
        private readonly IBackgroundJobExternal _backgroundJobExternal;

        public ProjectDeadlineService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<ProjectDeadlineService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IProjectDeadlineQuery projectDeadlineQuery, IUserQuery userQuery, ProjectConfigDomainService projectConfigDomainService, IBackgroundJobExternal backgroundJobExternal
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _projectDeadlineQuery = projectDeadlineQuery;
            _userQuery = userQuery;
            _projectConfigDomainService = projectConfigDomainService;
            _backgroundJobExternal = backgroundJobExternal;
        }

        public async Task<BaseResponsePagingDto<GetListProjectDeadlineDto>> FindPaging(FindPagingProjectDeadlineDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.NotificationType.HasValue && !ProjectDeadlineConst.NOTIFICATION_TYPES.Contains(dto.NotificationType.Value))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Loại hạn chỉ nhận 1 (sắp hết hạn) hoặc 2 (đã hết hạn)");
            }
            var settings = await _projectConfigDomainService.GetDeadlineSettingsAsync();
            return await _projectDeadlineQuery.FindPaging(dto, GetVietnamTime().Date, settings.WarningDays);
        }

        public async Task<ViewRunDeadlineCheckDto> Run()
        {
            _logger.LogInformation($"{nameof(Run)}");
            var jobId = _backgroundJobExternal.TriggerRecurring(ProjectDeadlineConst.RECURRING_JOB_ID)
                ?? throw new UserFriendlyException(ErrorCodes.BadRequest, "Job kiểm tra hạn dự án chưa được đăng ký");

            AddAuditLog(AuditLogConstants.ACTION_PROJECT_DEADLINE_RUN, AuditLogConstants.ACTION_TYPE_UPDATE,
                "Chạy kiểm tra hạn dự án thủ công", newData: new { JobId = jobId });
            await _unitOfWork.SaveChangesAsync();

            return new ViewRunDeadlineCheckDto { JobId = jobId };
        }

        public async Task<ViewDeadlineCheckStatusDto> FindStatus()
        {
            _logger.LogInformation($"{nameof(FindStatus)}");
            var settings = await _projectConfigDomainService.GetDeadlineSettingsAsync();
            return new ViewDeadlineCheckStatusDto
            {
                Job = _backgroundJobExternal.FindRecurring(ProjectDeadlineConst.RECURRING_JOB_ID),
                WarningDays = settings.WarningDays,
                MailEnabled = settings.IsMailEnabled,
                Recipients = await _userQuery.GetEmailsByRoleNamesAsync([RoleConstants.ROLE_SUPER_ADMIN, RoleConstants.ROLE_ADMIN]),
            };
        }

        public async Task<BaseResponsePagingDto<GetListProjectDeadlineNotificationDto>> FindNotificationPaging(FindPagingProjectDeadlineNotificationDto dto)
        {
            _logger.LogInformation($"{nameof(FindNotificationPaging)}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.FromDate.HasValue && dto.ToDate.HasValue && dto.FromDate.Value.Date > dto.ToDate.Value.Date)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Từ ngày không được sau đến ngày");
            }
            return await _projectDeadlineQuery.FindNotificationPaging(dto);
        }
    }
}
