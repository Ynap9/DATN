using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Auth;
using sh.be.domain.Module.Core;
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
                .Where(r => !r.Deleted && FullPermissionRoles.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync();

            var permissions = await dbContext.Permissions
                .Where(p => !p.Deleted)
                .Select(p => new { p.Key, p.ParentKey })
                .ToListAsync();

            var existingPairs = (await dbContext.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => new { rp.RoleId, rp.PermissionKey })
                .ToListAsync())
                .Select(rp => (rp.RoleId, rp.PermissionKey))
                .ToHashSet();

            var newRolePermissions = (
                from roleId in roleIds
                from permission in permissions
                where !existingPairs.Contains((roleId, permission.Key))
                select new RolePermission
                {
                    RoleId = roleId,
                    PermissionParentKey = permission.ParentKey ?? String.Empty,
                    PermissionKey = permission.Key,
                    CreatedBy = "system",
                })
                .ToList();

            if (newRolePermissions.Count == 0)
            {
                return;
            }

            dbContext.RolePermissions.AddRange(newRolePermissions);
            dbContext.AuditLogs.Add(new AuditLog
            {
                Action = AuditLogConstants.ACTION_SEED_ROLE_PERMISSION,
                ActionType = AuditLogConstants.ACTION_TYPE_CREATE,
                Content = $"Khởi tạo quyền cho vai trò mặc định: {string.Join(", ", FullPermissionRoles)}",
                NewData = AuditLogUtils.ToJson(newRolePermissions.Select(rp => new { rp.RoleId, rp.PermissionKey })),
                Source = nameof(SeedRolePermission),
                CreatedDate = DateTimeUtils.GetVietnamTime(),
            });
            await dbContext.SaveChangesAsync();
        }
    }
}
