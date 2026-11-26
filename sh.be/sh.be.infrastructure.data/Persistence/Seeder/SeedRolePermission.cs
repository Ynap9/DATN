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

        private static readonly string[] StaffPermissions =
        [
            PermissionConstants.FUNCTION_PROJECT_VIEW,
            PermissionConstants.FUNCTION_DOCUMENT_TYPE_VIEW,
            PermissionConstants.FUNCTION_DOCUMENT_FIELD_VIEW,
            PermissionConstants.FUNCTION_SUBMISSION_VIEW,
            PermissionConstants.FUNCTION_SUBMISSION_PROGRESS_STATS,
            PermissionConstants.FUNCTION_SUBMISSION_QA_STATS,
            PermissionConstants.FUNCTION_ASSIGNMENT_VIEW_OWN,
            PermissionConstants.FUNCTION_WAGE_VIEW,
        ];

        private static readonly Dictionary<string, string[]> DefaultPermissionRoles = new()
        {
            [RoleConstants.ROLE_LEADER] =
            [
                PermissionConstants.FUNCTION_PROJECT_VIEW,
                PermissionConstants.FUNCTION_DOCUMENT_TYPE_VIEW,
                PermissionConstants.FUNCTION_DOCUMENT_FIELD_VIEW,
                PermissionConstants.FUNCTION_SUBMISSION_VIEW,
                PermissionConstants.FUNCTION_SUBMISSION_IMPORT,
                PermissionConstants.FUNCTION_SUBMISSION_EXPORT,
                PermissionConstants.FUNCTION_SUBMISSION_CHECK,
                PermissionConstants.FUNCTION_SUBMISSION_PROGRESS_STATS,
                PermissionConstants.FUNCTION_SUBMISSION_QA_STATS,
                PermissionConstants.FUNCTION_STAFF_GROUP_MANAGE,
                PermissionConstants.FUNCTION_PROJECT_MEMBER_MANAGE,
                PermissionConstants.FUNCTION_PRICE_MANAGE_ACTUAL,
                PermissionConstants.FUNCTION_ASSIGNMENT_MANAGE,
                PermissionConstants.FUNCTION_ASSIGNMENT_VIEW_ALL_SUBMISSION,
                PermissionConstants.FUNCTION_WAGE_VIEW,
            ],
            [RoleConstants.ROLE_DATA] = [.. StaffPermissions, PermissionConstants.FUNCTION_SUBMISSION_CHECK],
            [RoleConstants.ROLE_QA] = [.. StaffPermissions, PermissionConstants.FUNCTION_SUBMISSION_QA],
        };

        public static async Task SeedAsync(ShDbContext dbContext)
        {
            await SeedFullPermissionRolesAsync(dbContext);
            await SeedDefaultPermissionRolesAsync(dbContext);
        }

        private static async Task SeedFullPermissionRolesAsync(ShDbContext dbContext)
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

        // Vai trò Leader / Data / QA chưa có dòng quyền nào (kể cả xoá mềm) thì cấp bộ quyền mặc định; đã có thì giữ nguyên
        private static async Task SeedDefaultPermissionRolesAsync(ShDbContext dbContext)
        {
            var roles = await dbContext.Roles
                .Where(r => DefaultPermissionRoles.Keys.Contains(r.Name))
                .ToListAsync();

            var seededRoleIds = (await dbContext.RolePermissions
                .IgnoreQueryFilters()
                .Where(rp => roles.Select(r => r.Id).Contains(rp.RoleId))
                .Select(rp => rp.RoleId)
                .Distinct()
                .ToListAsync())
                .ToHashSet();

            var permissions = await dbContext.Permissions.ToListAsync();
            var now = DateTimeUtils.GetVietnamTime();
            var hasChanges = false;

            foreach (var role in roles.Where(r => !seededRoleIds.Contains(r.Id)))
            {
                var keys = DefaultPermissionRoles[role.Name].Where(key => permissions.Any(p => p.Key == key));
                var rolePermissions = role.AssignPermissions(permissions, keys, "system", now);

                dbContext.RolePermissions.AddRange(rolePermissions);
                dbContext.AuditLogs.Add(AuditLog.Create(
                    null,
                    AuditLogConstants.ACTION_SEED_ROLE_PERMISSION,
                    AuditLogConstants.ACTION_TYPE_CREATE,
                    $"Khởi tạo quyền mặc định cho vai trò {role.Name}",
                    null,
                    rolePermissions.Select(rp => new { rp.RoleId, rp.PermissionKey }),
                    nameof(SeedRolePermission),
                    null,
                    now));
                hasChanges = true;
            }

            if (hasChanges)
            {
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
