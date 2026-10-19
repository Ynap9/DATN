using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Auth.Dtos.User;
using sh.be.applications.Module.Auth.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Auth
{
    [Route("api/v{version:apiVersion}/auth/user")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserController:BaseController
    {
        private readonly IUserService _userService;
        public UserController(ILogger<UserController> logger, IUserService userService):base(logger)
        {
            _userService = userService;
        }

        [HttpGet("me")]
        public ApiResponse GetMe()
        {
            try
            {
                var data =_userService.GetMe();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("paging")]
        [Permission(PermissionConstants.MENU_AUTH)]
        public async Task<ApiResponse> FindPaging([FromQuery] FindPagingUserDto dto)
        {
            try
            {
                var data = await _userService.FindPaging(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("")]
        [Permission(PermissionConstants.MENU_AUTH)]
        public async Task<ApiResponse> Create([FromBody] CreateUserDto dto){
            try
            {
                await _userService.Create(dto);
                return new();
            }catch(Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("")]
        [Permission(PermissionConstants.MENU_AUTH)]
        public async Task<ApiResponse> Update([FromBody] UpdateUserDto dto)
        {
            try
            {
                await _userService.Update(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpDelete("{userid}")]
        [Permission(PermissionConstants.MENU_AUTH)]
        public async Task<ApiResponse> Delete([FromRoute]string userId)
        {
            try
            {
                await _userService.Delete(userId);
                return new();
            }catch(Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("change-password")]
        public async Task<ApiResponse> ChangePassword([FromBody]ChangePasswordDto dto)
        {
            try
            {
                await _userService.ChangePassword(dto);
                return new();
            }catch(Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("role")]
        [Permission(PermissionConstants.MENU_AUTH)]
        public async Task<ApiResponse> UpdateRole ([FromBody] UpdateRoleUserDto dto)
        {
            try
            {
                await _userService.UpdateRoleUser(dto);
                return new();
            }catch(Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
