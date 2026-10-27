using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Operation.Dtos.ProjectMember;
using sh.be.applications.Module.Operation.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Operation
{
    [Route("api/v{version:apiVersion}/operation/project/{projectId:int}")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ProjectMemberController : BaseController
    {
        private readonly IProjectMemberService _projectMemberService;
        public ProjectMemberController(ILogger<ProjectMemberController> logger, IProjectMemberService projectMemberService) : base(logger)
        {
            _projectMemberService = projectMemberService;
        }

        [HttpGet("member/paging")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_MEMBER_MANAGE)]
        public async Task<ApiResponse> FindPaging([FromRoute] int projectId, [FromQuery] FindPagingProjectMemberDto dto)
        {
            try
            {
                var data = await _projectMemberService.FindPaging(projectId, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("member/candidates")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_MEMBER_MANAGE)]
        public async Task<ApiResponse> FindCandidates([FromRoute] int projectId, [FromQuery] FindProjectMemberCandidateDto dto)
        {
            try
            {
                var data = await _projectMemberService.FindCandidates(projectId, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("leader")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_MEMBER_MANAGE)]
        public async Task<ApiResponse> AddLeaders([FromRoute] int projectId, [FromBody] AddProjectLeaderDto dto)
        {
            try
            {
                var data = await _projectMemberService.AddLeaders(projectId, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("member")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_MEMBER_MANAGE)]
        public async Task<ApiResponse> AddMembers([FromRoute] int projectId, [FromBody] AddProjectMemberDto dto)
        {
            try
            {
                var data = await _projectMemberService.AddMembers(projectId, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("member/{memberId:int}")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_MEMBER_MANAGE)]
        public async Task<ApiResponse> Update([FromRoute] int projectId, [FromRoute] int memberId, [FromBody] UpdateProjectMemberDto dto)
        {
            try
            {
                var data = await _projectMemberService.Update(projectId, memberId, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpDelete("member/{memberId:int}")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_MEMBER_MANAGE)]
        public async Task<ApiResponse> Remove([FromRoute] int projectId, [FromRoute] int memberId)
        {
            try
            {
                var data = await _projectMemberService.Remove(projectId, memberId);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
