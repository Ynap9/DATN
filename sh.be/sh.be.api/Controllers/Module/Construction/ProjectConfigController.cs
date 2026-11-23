using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Construction.Dtos.ProjectConfig;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Settings;

namespace sh.be.api.Controllers.Module.Construction
{
    [Route("api/v{version:apiVersion}/construction/project-config")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ProjectConfigController : BaseController
    {
        private const string ProjectConfigPagePath = "/admin/construction/project-config";
        private const int GoogleErrorMessageMaxLength = 200;
        private const string GoogleAuthorizationCanceledMessage = "Bạn đã huỷ cấp quyền Google";
        private const string GoogleAuthorizationFailedMessage = "Kết nối Google thất bại, thử lại sau";

        private readonly IProjectConfigService _projectConfigService;
        private readonly AuthServerSettings _authServerSettings;
        private readonly ILogger<ProjectConfigController> _logger;

        public ProjectConfigController(ILogger<ProjectConfigController> logger, IProjectConfigService projectConfigService,
            IOptions<AuthServerSettings> authServerOptions) : base(logger)
        {
            _projectConfigService = projectConfigService;
            _authServerSettings = authServerOptions.Value;
            _logger = logger;
        }

        [HttpGet("")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_CONFIG_VIEW)]
        public async Task<ApiResponse> GetList()
        {
            try
            {
                var data = await _projectConfigService.GetList();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_CONFIG_VIEW)]
        public async Task<ApiResponse> FindById([FromRoute] int id)
        {
            try
            {
                var data = await _projectConfigService.FindById(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_CONFIG_CREATE)]
        public async Task<ApiResponse> Create([FromBody] CreateProjectConfigDto dto)
        {
            try
            {
                await _projectConfigService.Create(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("{id}")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_CONFIG_UPDATE)]
        public async Task<ApiResponse> Update([FromRoute] int id, [FromBody] UpdateProjectConfigDto dto)
        {
            try
            {
                await _projectConfigService.Update(id, dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpDelete("{id}")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_CONFIG_DELETE)]
        public async Task<ApiResponse> Delete([FromRoute] int id)
        {
            try
            {
                await _projectConfigService.Delete(id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("google-authorization-url")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_CONFIG_UPDATE)]
        public async Task<ApiResponse> GetGoogleAuthorizationUrl()
        {
            try
            {
                var data = await _projectConfigService.GetGoogleAuthorizationUrl();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("google-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> CompleteGoogleAuthorization([FromQuery] GoogleAuthorizationCallbackDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Error))
            {
                return RedirectToProjectConfig(GoogleAuthorizationCanceledMessage);
            }

            try
            {
                await _projectConfigService.CompleteGoogleAuthorization(dto);
                return RedirectToProjectConfig();
            }
            catch (UserFriendlyException ex)
            {
                return RedirectToProjectConfig(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(CompleteGoogleAuthorization)}");
                return RedirectToProjectConfig(GoogleAuthorizationFailedMessage);
            }
        }

        // Về màn Cấu hình: không lỗi → ?google=success, có lỗi → ?google=error&message=<câu lỗi tối đa 200 ký tự>
        private RedirectResult RedirectToProjectConfig(string? errorMessage = null)
        {
            var url = $"{_authServerSettings.AppUrl.TrimEnd('/')}{ProjectConfigPagePath}";
            if (errorMessage == null)
            {
                return Redirect($"{url}?google=success");
            }

            var message = errorMessage.Length > GoogleErrorMessageMaxLength ? errorMessage[..GoogleErrorMessageMaxLength] : errorMessage;
            return Redirect($"{url}?google=error&message={Uri.EscapeDataString(message)}");
        }
    }
}
