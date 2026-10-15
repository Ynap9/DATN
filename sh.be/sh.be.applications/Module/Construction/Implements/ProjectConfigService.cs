using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.Spreadsheet;
using sh.be.applications.Base.Interfaces;
using sh.be.applications.Module.Construction.Dtos.ProjectConfig;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Construction.Entities;
using sh.be.domain.Module.Construction.Repositories;
using sh.be.domain.Module.Construction.Services;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Core;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.applications.Module.Construction.Implements
{
    public class ProjectConfigService : BaseService, IProjectConfigService
    {
        private readonly IProjectConfigRepository _projectConfigRepository;
        private readonly IProjectConfigQuery _projectConfigQuery;
        private readonly ProjectConfigDomainService _projectConfigDomainService;
        private readonly ISpreadsheetAuthorizer _spreadsheetAuthorizer;
        private readonly IAuthorizationStateProtector _authorizationStateProtector;

        public ProjectConfigService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<ProjectConfigService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IProjectConfigRepository projectConfigRepository, IProjectConfigQuery projectConfigQuery, ProjectConfigDomainService projectConfigDomainService,
            ISpreadsheetAuthorizer spreadsheetAuthorizer, IAuthorizationStateProtector authorizationStateProtector
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _projectConfigRepository = projectConfigRepository;
            _projectConfigQuery = projectConfigQuery;
            _projectConfigDomainService = projectConfigDomainService;
            _spreadsheetAuthorizer = spreadsheetAuthorizer;
            _authorizationStateProtector = authorizationStateProtector;
        }

        public async Task<List<GetListProjectConfigDto>> GetList()
        {
            _logger.LogInformation($"{nameof(GetList)}");
            return await _projectConfigQuery.GetList();
        }

        public async Task<FindProjectConfigByIdDto> FindById(int id)
        {
            _logger.LogInformation($"{nameof(FindById)}, id={id}");
            return await _projectConfigQuery.FindById(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectConfigNotFound);
        }

        public async Task Create(CreateProjectConfigDto dto)
        {
            _logger.LogInformation($"{nameof(Create)}, name={dto.Name}");
            await _projectConfigDomainService.EnsureUniqueNameAsync(dto.Name);

            var projectConfig = ProjectConfig.Create(dto.Name, dto.Value, dto.Description, getCurrentUserId(), GetVietnamTime());

            _projectConfigRepository.Add(projectConfig);
            AddAuditLog(AuditLogConstants.ACTION_PROJECT_CONFIG_CREATE, AuditLogConstants.ACTION_TYPE_CREATE, $"Tạo cấu hình dự án {projectConfig.Name}",
                null, ToAuditData(projectConfig));
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task Update(int id, UpdateProjectConfigDto dto)
        {
            _logger.LogInformation($"{nameof(Update)}, id={id}, name={dto.Name}");
            var projectConfig = await _projectConfigRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectConfigNotFound);
            var oldData = ToAuditData(projectConfig);

            await _projectConfigDomainService.EnsureUniqueNameAsync(dto.Name, id);
            projectConfig.Update(dto.Name, dto.Value, dto.Description, getCurrentUserId(), GetVietnamTime());

            _projectConfigRepository.Update(projectConfig);
            AddAuditLog(AuditLogConstants.ACTION_PROJECT_CONFIG_UPDATE, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật cấu hình dự án {projectConfig.Name}",
                oldData, ToAuditData(projectConfig));
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            _logger.LogInformation($"{nameof(Delete)}, id={id}");
            var projectConfig = await _projectConfigRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectConfigNotFound);

            projectConfig.Delete(getCurrentUserId(), GetVietnamTime());

            _projectConfigRepository.Update(projectConfig);
            AddAuditLog(AuditLogConstants.ACTION_PROJECT_CONFIG_DELETE, AuditLogConstants.ACTION_TYPE_DELETE, $"Xóa cấu hình dự án {projectConfig.Name}",
                ToAuditData(projectConfig), null);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<string> GetGoogleAuthorizationUrl()
        {
            _logger.LogInformation($"{nameof(GetGoogleAuthorizationUrl)}");
            var account = await GetGoogleClientAsync();
            return _spreadsheetAuthorizer.BuildAuthorizationUrl(account, _authorizationStateProtector.CreateState(getCurrentUserId()));
        }

        public async Task CompleteGoogleAuthorization(GoogleAuthorizationCallbackDto dto)
        {
            _logger.LogInformation($"{nameof(CompleteGoogleAuthorization)}");
            var userId = _authorizationStateProtector.ReadUserId(dto.State);

            var account = await GetGoogleClientAsync();
            var refreshToken = await _spreadsheetAuthorizer.ExchangeRefreshTokenAsync(account, dto.Code ?? String.Empty);
            var projectConfig = await _projectConfigDomainService.SaveValueAsync(ProjectConfigConst.GOOGLE_REFRESH_TOKEN, refreshToken,
                userId, GetVietnamTime());

            AddAuditLog(AuditLogConstants.ACTION_PROJECT_CONFIG_AUTHORIZE_GOOGLE, AuditLogConstants.ACTION_TYPE_UPDATE, "Xác thực tài khoản Google cho cấu hình dự án",
                null, ToAuditData(projectConfig), userId);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<SpreadsheetAccountDto> GetGoogleClientAsync()
        {
            var values = await _projectConfigDomainService.GetRequiredValuesAsync(
                ProjectConfigConst.GOOGLE_CLIENT_ID, ProjectConfigConst.GOOGLE_CLIENT_SECRET, ProjectConfigConst.GOOGLE_REDIRECT_URI);

            return new SpreadsheetAccountDto
            {
                ClientId = values[ProjectConfigConst.GOOGLE_CLIENT_ID],
                ClientSecret = values[ProjectConfigConst.GOOGLE_CLIENT_SECRET],
                RedirectUri = values[ProjectConfigConst.GOOGLE_REDIRECT_URI],
            };
        }

        private static object ToAuditData(ProjectConfig projectConfig)
        {
            return new
            {
                projectConfig.Id,
                projectConfig.Name,
                Value = projectConfig.IsSecret() ? ProjectConfigConst.MASKED_VALUE : projectConfig.Value,
                projectConfig.Description,
            };
        }
    }
}
