using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Auth.Dtos.Role;
using sh.be.applications.Module.Auth.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Auth
{
    [Route("api/auth/role")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class RoleController:BaseController
    {
        private readonly IRoleService _roleService;
        public RoleController(ILogger<RoleController> logger,IRoleService roleService) : base(logger)
        {
            _roleService = roleService;
        }
        [HttpGet("dropdown")]
        [Permission(PermissionConstants.FUNCTION_ROLE_VIEW)]
        public ApiResponse GetListRoleDropDown()
        {
            try
            {
                var data = _roleService.GetRoleDropDown();
                return new (data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}")]
        [Permission(PermissionConstants.FUNCTION_ROLE_VIEW)]
        public ApiResponse FindById([FromRoute]int id)
        {
            try
            {
                var data = _roleService.FindById(id);
                return new(data);
            }catch(Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("paging")]
        [Permission(PermissionConstants.MENU_AUTH)]
        public ApiResponse FindPaging([FromQuery] FindPagingRoleDto dto)
        {
            try
            {
                var data = _roleService.FindPaging(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}/tree")]
        [Permission(PermissionConstants.FUNCTION_ROLE_VIEW)]
        public ApiResponse GetPermissionTree([FromRoute] int id)
        {
            try
            {
                var data = _roleService.GetPermissionTree(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("permission")]
        [Permission(PermissionConstants.FUNCTION_ROLE_ASSIGN_PERMISSION)]
        public async Task<ApiResponse> UpdatePermission([FromBody] UpdateRolePermissionDto dto)
        {
            try
            {
                await _roleService.UpdatePermission(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }


        [HttpPost("")]
        [Permission(PermissionConstants.FUNCTION_ROLE_CREATE)]
        public ApiResponse Create([FromBody] CreateRoleDto dto)
        {
            try
            {
                _roleService.Create(dto);
                return new();
            }
            catch (Exception ex) { 
                return OkException(ex);
            }
        }
        [HttpDelete("{id}")]
        [Permission(PermissionConstants.FUNCTION_ROLE_DELETE)]
        public ApiResponse Delete([FromRoute] int id)
        {
            try
            {
                _roleService.Delete(id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }


        [HttpPut("{id}")]
        [Permission(PermissionConstants.FUNCTION_ROLE_UPDATE)]
        public ApiResponse Update([FromRoute]int id, [FromBody] UpdateRoleDto dto)
        {
            try
            {
                _roleService.Update(id, dto);
                return new();
            }catch(Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
