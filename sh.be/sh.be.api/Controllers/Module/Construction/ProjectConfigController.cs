using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Construction.Dtos.ProjectConfig;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Construction
{
    [Route("api/v{version:apiVersion}/construction/project-config")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ProjectConfigController : BaseController
    {
        private readonly IProjectConfigService _projectConfigService;
        public ProjectConfigController(ILogger<ProjectConfigController> logger, IProjectConfigService projectConfigService) : base(logger)
        {
            _projectConfigService = projectConfigService;
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
        public async Task<ApiResponse> CompleteGoogleAuthorization([FromQuery] GoogleAuthorizationCallbackDto dto)
        {
            try
            {
                await _projectConfigService.CompleteGoogleAuthorization(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
