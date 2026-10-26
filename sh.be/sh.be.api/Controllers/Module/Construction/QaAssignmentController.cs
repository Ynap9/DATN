using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Construction.Dtos.QaAssignment;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Construction
{
    [Route("api/v{version:apiVersion}/construction/qa-assignment")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class QaAssignmentController : BaseController
    {
        private readonly IQaAssignmentService _qaAssignmentService;
        public QaAssignmentController(ILogger<QaAssignmentController> logger, IQaAssignmentService qaAssignmentService) : base(logger)
        {
            _qaAssignmentService = qaAssignmentService;
        }

        [HttpGet("config")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_CONFIG_VIEW)]
        public async Task<ApiResponse> FindConfig([FromQuery] int projectId)
        {
            try
            {
                var data = await _qaAssignmentService.FindConfig(projectId);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("config")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_CONFIG_UPDATE)]
        public async Task<ApiResponse> UpdateConfig([FromBody] UpdateQaSamplingConfigDto dto)
        {
            try
            {
                await _qaAssignmentService.UpdateConfig(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("reviewer-mode")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_CONFIG_VIEW)]
        public async Task<ApiResponse> FindReviewerModes([FromQuery] int projectId)
        {
            try
            {
                var data = await _qaAssignmentService.FindReviewerModes(projectId);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("reviewer-mode")]
        [Permission(PermissionConstants.FUNCTION_PROJECT_CONFIG_UPDATE)]
        public async Task<ApiResponse> UpdateReviewerMode([FromBody] UpdateQaReviewerModeDto dto)
        {
            try
            {
                await _qaAssignmentService.UpdateReviewerMode(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
