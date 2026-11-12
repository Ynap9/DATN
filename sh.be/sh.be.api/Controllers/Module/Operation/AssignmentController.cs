using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Operation.Dtos.Assignment;
using sh.be.applications.Module.Operation.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Operation
{
    [Route("api/v{version:apiVersion}/operation")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AssignmentController : BaseController
    {
        private readonly IAssignmentService _assignmentService;
        public AssignmentController(ILogger<AssignmentController> logger, IAssignmentService assignmentService) : base(logger)
        {
            _assignmentService = assignmentService;
        }

        [HttpGet("project/{projectId:int}/document-type/{documentTypeId:int}/assignment-tree")]
        [Permission(PermissionConstants.FUNCTION_ASSIGNMENT_MANAGE)]
        public async Task<ApiResponse> FindTree([FromRoute] int projectId, [FromRoute] int documentTypeId, [FromQuery] FindAssignmentTreeDto dto)
        {
            try
            {
                var data = await _assignmentService.FindTree(projectId, documentTypeId, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("project/{projectId:int}/document-type/{documentTypeId:int}/assignment-tree/profiles")]
        [Permission(PermissionConstants.FUNCTION_ASSIGNMENT_MANAGE)]
        public async Task<ApiResponse> FindProfiles([FromRoute] int projectId, [FromRoute] int documentTypeId, [FromQuery] FindAssignmentProfilesDto dto)
        {
            try
            {
                var data = await _assignmentService.FindProfiles(projectId, documentTypeId, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("project/{projectId:int}/document-type/{documentTypeId:int}/assignment/preview")]
        [Permission(PermissionConstants.FUNCTION_ASSIGNMENT_MANAGE)]
        public async Task<ApiResponse> Preview([FromRoute] int projectId, [FromRoute] int documentTypeId, [FromBody] AssignmentRequestDto dto)
        {
            try
            {
                var data = await _assignmentService.Preview(projectId, documentTypeId, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("project/{projectId:int}/document-type/{documentTypeId:int}/assignment")]
        [Permission(PermissionConstants.FUNCTION_ASSIGNMENT_MANAGE)]
        public async Task<ApiResponse> Assign([FromRoute] int projectId, [FromRoute] int documentTypeId, [FromBody] AssignmentRequestDto dto)
        {
            try
            {
                var data = await _assignmentService.Assign(projectId, documentTypeId, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("project/{projectId:int}/document-type/{documentTypeId:int}/assignment/{ticketId:int}")]
        [Permission(PermissionConstants.FUNCTION_ASSIGNMENT_MANAGE)]
        public async Task<ApiResponse> FindTicketById([FromRoute] int projectId, [FromRoute] int documentTypeId, [FromRoute] int ticketId)
        {
            try
            {
                var data = await _assignmentService.FindTicketById(projectId, documentTypeId, ticketId);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("my-assignment")]
        [Permission(PermissionConstants.FUNCTION_ASSIGNMENT_VIEW_OWN)]
        public async Task<ApiResponse> FindMyAssignments([FromQuery] FindMyAssignmentDto dto)
        {
            try
            {
                var data = await _assignmentService.FindMyAssignments(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
