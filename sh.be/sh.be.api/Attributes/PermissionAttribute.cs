using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using sh.be.applications.Module.Auth.Dtos.Role;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Utils;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
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

            var dbContext = context.HttpContext
                .RequestServices
                .GetRequiredService(typeof(ShDbContext)) as ShDbContext;

            if(dbContext != null)
            {
                var isSuperAdmin = await (from u in dbContext.Users
                                    join ur in dbContext.UserRoles on u.Id equals ur.UserId
                                    join r in dbContext.Roles on ur.RoleId equals r.Id
                                    where u.Id == userId && r.Name == RoleConstants.ROLE_SUPER_ADMIN && !r.Deleted && !ur.Deleted
                                    select r.Name).AnyAsync();
                if (isSuperAdmin)
                {
                    return;
                }

                var grantedKeys = await (from ur in dbContext.UserRoles
                                         join r in dbContext.Roles on ur.RoleId equals r.Id
                                         join rp in dbContext.RolePermissions on ur.RoleId equals rp.RoleId
                                         where ur.UserId == userId
                                          && !r.Deleted && !ur.Deleted && !rp.Deleted
                                         select rp.PermissionKey).Distinct().ToListAsync();

                var permissions = await dbContext.Permissions
                    .AsNoTracking()
                    .Where(p => !p.Deleted)
                    .Select(p => new GetRolePermissionTreeDto
                    {
                        Key = p.Key,
                        ParentKey = p.ParentKey,
                        IsChecked = grantedKeys.Contains(p.Key),
                    }).ToListAsync();

                var tree = TreeUtils.BuildTree(permissions, p => p.Key, p => p.ParentKey, p => p.Children);
                TreeUtils.SyncChecked(tree, p => p.Children, p => p.IsChecked, (p, isChecked) => p.IsChecked = isChecked);

                var isPermit = permissions.Any(p => p.IsChecked && Permissions.Contains(p.Key));
               if(!isPermit)
                {
                    context.Result = new ForbidResult(JwtBearerDefaults.AuthenticationScheme);
                }
            }

        }
    }
}
