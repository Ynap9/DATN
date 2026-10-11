using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Auth.Entities;
using sh.be.domain.Module.Core.Entities;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Core;
using sh.be.shared.Utils;

namespace sh.be.infrastructure.data.Persistence.Seeder
{
    public static class SeedRolePermission
    {
        private static readonly string[] FullPermissionRoles =
        [
            RoleConstants.ROLE_SUPER_ADMIN,
            RoleConstants.ROLE_ADMIN,
        ];

        public static async Task SeedAsync(ShDbContext dbContext)
        {
            var roleIds = await dbContext.Roles
                .Where(r => FullPermissionRoles.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync();

            var permissions = await dbContext.Permissions
                .Select(p => new { p.Key, p.ParentKey })
                .ToListAsync();

            var existingPairs = (await dbContext.RolePermissions
                .IgnoreQueryFilters()
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => new { rp.RoleId, rp.PermissionKey })
                .ToListAsync())
                .Select(rp => (rp.RoleId, rp.PermissionKey))
                .ToHashSet();

            var newRolePermissions = (
                from roleId in roleIds
                from permission in permissions
                where !existingPairs.Contains((roleId, permission.Key))
                select RolePermission.Create(roleId, permission.Key, permission.ParentKey ?? String.Empty, "system", null))
                .ToList();

            if (newRolePermissions.Count == 0)
            {
                return;
            }

            dbContext.RolePermissions.AddRange(newRolePermissions);
            dbContext.AuditLogs.Add(AuditLog.Create(
                null,
                AuditLogConstants.ACTION_SEED_ROLE_PERMISSION,
                AuditLogConstants.ACTION_TYPE_CREATE,
                $"Khởi tạo quyền cho vai trò mặc định: {string.Join(", ", FullPermissionRoles)}",
                null,
                newRolePermissions.Select(rp => new { rp.RoleId, rp.PermissionKey }),
                nameof(SeedRolePermission),
                null,
                DateTimeUtils.GetVietnamTime()));
            await dbContext.SaveChangesAsync();
        }
    }
}
