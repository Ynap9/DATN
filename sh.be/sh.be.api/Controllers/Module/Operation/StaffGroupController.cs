using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Operation.Dtos.StaffGroup;
using sh.be.applications.Module.Operation.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Operation
{
    [Route("api/v{version:apiVersion}/operation/staff-group")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class StaffGroupController : BaseController
    {
        private readonly IStaffGroupService _staffGroupService;
        public StaffGroupController(ILogger<StaffGroupController> logger, IStaffGroupService staffGroupService) : base(logger)
        {
            _staffGroupService = staffGroupService;
        }

        [HttpGet("paging")]
        [Permission(PermissionConstants.FUNCTION_STAFF_GROUP_MANAGE)]
        public async Task<ApiResponse> FindPaging([FromQuery] FindPagingStaffGroupDto dto)
        {
            try
            {
                var data = await _staffGroupService.FindPaging(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("candidates")]
        [Permission(PermissionConstants.FUNCTION_STAFF_GROUP_MANAGE)]
        public async Task<ApiResponse> FindCandidates([FromQuery] FindStaffGroupCandidateDto dto)
        {
            try
            {
                var data = await _staffGroupService.FindCandidates(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id:int}")]
        [Permission(PermissionConstants.FUNCTION_STAFF_GROUP_MANAGE)]
        public async Task<ApiResponse> FindById([FromRoute] int id)
        {
            try
            {
                var data = await _staffGroupService.FindById(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("")]
        [Permission(PermissionConstants.FUNCTION_STAFF_GROUP_MANAGE)]
        public async Task<ApiResponse> Create([FromBody] CreateStaffGroupDto dto)
        {
            try
            {
                var data = await _staffGroupService.Create(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("{id:int}")]
        [Permission(PermissionConstants.FUNCTION_STAFF_GROUP_MANAGE)]
        public async Task<ApiResponse> Update([FromRoute] int id, [FromBody] UpdateStaffGroupDto dto)
        {
            try
            {
                await _staffGroupService.Update(id, dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpDelete("{id:int}")]
        [Permission(PermissionConstants.FUNCTION_STAFF_GROUP_MANAGE)]
        public async Task<ApiResponse> Delete([FromRoute] int id)
        {
            try
            {
                await _staffGroupService.Delete(id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("{id:int}/members")]
        [Permission(PermissionConstants.FUNCTION_STAFF_GROUP_MANAGE)]
        public async Task<ApiResponse> AddMembers([FromRoute] int id, [FromBody] ChangeStaffGroupMembersDto dto)
        {
            try
            {
                var data = await _staffGroupService.AddMembers(id, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpDelete("{id:int}/members")]
        [Permission(PermissionConstants.FUNCTION_STAFF_GROUP_MANAGE)]
        public async Task<ApiResponse> RemoveMembers([FromRoute] int id, [FromBody] ChangeStaffGroupMembersDto dto)
        {
            try
            {
                var data = await _staffGroupService.RemoveMembers(id, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
