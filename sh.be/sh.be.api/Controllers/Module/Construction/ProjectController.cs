using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Construction.Dtos.Project;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Construction
{
    [Route("api/construction/project")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ProjectController : BaseController
    {
        private readonly IProjectService _projectService;
        public ProjectController(ILogger<ProjectController> logger, IProjectService projectService) : base(logger)
        {
            _projectService = projectService;
        }

        [HttpGet("dropdown")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_VIEW)]
        public async Task<ApiResponse> GetProjectDropDown()
        {
            try
            {
                var data = await _projectService.GetProjectDropDown();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("paging")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_VIEW)]
        public async Task<ApiResponse> FindPaging([FromQuery] FindPagingProjectDto dto)
        {
            try
            {
                var data = await _projectService.FindPaging(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_VIEW)]
        public async Task<ApiResponse> FindById([FromRoute] int id)
        {
            try
            {
                var data = await _projectService.FindById(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_CREATE)]
        public async Task<ApiResponse> Create([FromBody] CreateProjectDto dto)
        {
            try
            {
                var data = await _projectService.Create(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("{id}")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_UPDATE)]
        public async Task<ApiResponse> Update([FromRoute] int id, [FromBody] UpdateProjectDto dto)
        {
            try
            {
                await _projectService.Update(id, dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpDelete("{id}")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_DELETE)]
        public async Task<ApiResponse> Delete([FromRoute] int id)
        {
            try
            {
                await _projectService.Delete(id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
