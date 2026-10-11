using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using sh.be.applications.Module.Auth.Interfaces.Services;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace sh.be.api.Attributes

{
    public class PermissionAttribute: AuthorizeAttribute, IAsyncAuthorizationFilter
    {
        public string[] Permissions { get; }
        public PermissionAttribute(params string[] permissions)
        {
            Permissions = permissions;
        }


        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if(user.Identity!.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var userName = user.FindFirstValue("username");
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(Claims.Subject);
            if(string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var permissionService = context.HttpContext
                .RequestServices
                .GetRequiredService<IPermissionService>();

            var isPermit = await permissionService.HasAnyPermission(userId, Permissions);
            if(!isPermit)
            {
                context.Result = new ForbidResult(JwtBearerDefaults.AuthenticationScheme);
            }
        }
    }
}
