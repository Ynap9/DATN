using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Auth.Implements;
using sh.be.applications.Module.Auth.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;
using System.Security;

namespace sh.be.api.Controllers.Module.Auth
{
    [Route("api/v{version:apiVersion}/auth/permission")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PermissionController : BaseController
    {
        private readonly IPermissionService _permissionService;
        public PermissionController(ILogger<PermissionController> logger, IPermissionService permissionService) : base(logger)
        {
            _permissionService = permissionService;
        }

        [HttpGet("")]
        [Permission(PermissionConstants.MENU_AUTH)]
        public ApiResponse GetListPermission()
        {
            try
            {
                var data = _permissionService.GetListPermission();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
