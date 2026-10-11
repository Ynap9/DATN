using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Auth.Entities;
using sh.be.domain.Module.Core.Entities;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Core;
using sh.be.shared.Utils;

namespace sh.be.infrastructure.data.Persistence.Seeder
{
    public static class SeedPermission
    {
        private static readonly Permission[] DefaultPermissions =
        [
            Permission.Create(PermissionConstants.MENU_AUTH, "Menu quản trị hệ thống", null, PermissionConstants.MODULE_AUTH, null),

            Permission.Create(PermissionConstants.MENU_USER, "Menu quản lý người dùng", PermissionConstants.MENU_AUTH, PermissionConstants.MODULE_AUTH, null),
            Permission.Create(PermissionConstants.FUNCTION_USER_VIEW, "Xem danh sách và thông tin chi tiết người dùng", PermissionConstants.MENU_USER, PermissionConstants.MODULE_AUTH, null),
            Permission.Create(PermissionConstants.FUNCTION_USER_CREATE, "Tạo mới tài khoản người dùng", PermissionConstants.MENU_USER, PermissionConstants.MODULE_AUTH, null),
            Permission.Create(PermissionConstants.FUNCTION_USER_UPDATE, "Cập nhật thông tin và vai trò của người dùng", PermissionConstants.MENU_USER, PermissionConstants.MODULE_AUTH, null),
            Permission.Create(PermissionConstants.FUNCTION_USER_DELETE, "Xóa tài khoản người dùng", PermissionConstants.MENU_USER, PermissionConstants.MODULE_AUTH, null),

            Permission.Create(PermissionConstants.MENU_ROLE, "Menu quản lý vai trò", PermissionConstants.MENU_AUTH, PermissionConstants.MODULE_AUTH, null),
            Permission.Create(PermissionConstants.FUNCTION_ROLE_VIEW, "Xem danh sách và thông tin chi tiết vai trò", PermissionConstants.MENU_ROLE, PermissionConstants.MODULE_AUTH, null),
            Permission.Create(PermissionConstants.FUNCTION_ROLE_CREATE, "Tạo mới vai trò", PermissionConstants.MENU_ROLE, PermissionConstants.MODULE_AUTH, null),
            Permission.Create(PermissionConstants.FUNCTION_ROLE_UPDATE, "Cập nhật tên và mô tả vai trò", PermissionConstants.MENU_ROLE, PermissionConstants.MODULE_AUTH, null),
            Permission.Create(PermissionConstants.FUNCTION_ROLE_DELETE, "Xóa vai trò khỏi hệ thống", PermissionConstants.MENU_ROLE, PermissionConstants.MODULE_AUTH, null),
            Permission.Create(PermissionConstants.FUNCTION_ROLE_ASSIGN_PERMISSION, "Gán hoặc thu hồi quyền của vai trò", PermissionConstants.MENU_ROLE, PermissionConstants.MODULE_AUTH, null),

            Permission.Create(PermissionConstants.MENU_PERMISSION, "Menu quản lý quyền", PermissionConstants.MENU_AUTH, PermissionConstants.MODULE_AUTH, null),
            Permission.Create(PermissionConstants.FUNCTION_PERMISSION_VIEW, "Xem danh sách quyền trong hệ thống", PermissionConstants.MENU_PERMISSION, PermissionConstants.MODULE_AUTH, null),

            Permission.Create(PermissionConstants.MENU_CORE, "Menu hệ thống", null, PermissionConstants.MODULE_CORE, null),

            Permission.Create(PermissionConstants.MENU_AUDIT_LOG, "Menu nhật ký hệ thống", PermissionConstants.MENU_CORE, PermissionConstants.MODULE_CORE, null),
            Permission.Create(PermissionConstants.FUNCTION_AUDIT_LOG_VIEW, "Xem danh sách và thông tin chi tiết nhật ký hệ thống", PermissionConstants.MENU_AUDIT_LOG, PermissionConstants.MODULE_CORE, null),
        ];

        public static async Task SeedAsync(ShDbContext dbContext)
        {
            var defaultKeys = DefaultPermissions.Select(p => p.Key).ToHashSet();

            var existingPermissions = await dbContext.Permissions
                .IgnoreQueryFilters()
                .ToDictionaryAsync(p => p.Key);

            var obsoletePermissions = existingPermissions.Values
                .Where(p => !defaultKeys.Contains(p.Key))
                .ToList();

            if (obsoletePermissions.Count > 0)
            {
                var obsoleteKeys = obsoletePermissions.Select(p => p.Key).ToList();
                var obsoleteRolePermissions = await dbContext.RolePermissions
                    .IgnoreQueryFilters()
                    .Where(rp => obsoleteKeys.Contains(rp.PermissionKey))
                    .ToListAsync();

                dbContext.RolePermissions.RemoveRange(obsoleteRolePermissions);
                dbContext.Permissions.RemoveRange(obsoletePermissions);
            }

            foreach (var permission in DefaultPermissions)
            {
                if (!existingPermissions.TryGetValue(permission.Key, out var existing))
                {
                    dbContext.Permissions.Add(Permission.Create(permission.Key, permission.Description, permission.ParentKey, permission.Module, "system"));
                    continue;
                }

                existing.Sync(permission.Description, permission.ParentKey, permission.Module, "system", DateTime.Now);
            }

            if (dbContext.ChangeTracker.HasChanges())
            {
                var entries = dbContext.ChangeTracker.Entries<Permission>().ToList();
                dbContext.AuditLogs.Add(AuditLog.Create(
                    null,
                    AuditLogConstants.ACTION_SEED_PERMISSION,
                    AuditLogConstants.ACTION_TYPE_UPDATE,
                    "Đồng bộ danh sách quyền theo cấu hình hệ thống",
                    new
                    {
                        Deleted = entries.Where(e => e.State == EntityState.Deleted).Select(e => e.Entity.Key),
                    },
                    new
                    {
                        Added = entries.Where(e => e.State == EntityState.Added).Select(e => e.Entity.Key),
                        Modified = entries.Where(e => e.State == EntityState.Modified).Select(e => e.Entity.Key),
                    },
                    nameof(SeedPermission),
                    null,
                    DateTimeUtils.GetVietnamTime()));
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
