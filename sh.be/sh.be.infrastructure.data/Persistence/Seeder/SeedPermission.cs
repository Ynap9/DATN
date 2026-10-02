using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Auth;
using sh.be.domain.Module.Core;
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
            new() { Key = PermissionConstants.MENU_AUTH, Description = "Menu quản trị hệ thống", Module = PermissionConstants.MODULE_AUTH },

            new() { Key = PermissionConstants.MENU_USER, Description = "Menu quản lý người dùng", ParentKey = PermissionConstants.MENU_AUTH, Module = PermissionConstants.MODULE_AUTH },
            new() { Key = PermissionConstants.FUNCTION_USER_VIEW, Description = "Xem danh sách và thông tin chi tiết người dùng", ParentKey = PermissionConstants.MENU_USER, Module = PermissionConstants.MODULE_AUTH },
            new() { Key = PermissionConstants.FUNCTION_USER_CREATE, Description = "Tạo mới tài khoản người dùng", ParentKey = PermissionConstants.MENU_USER, Module = PermissionConstants.MODULE_AUTH },
            new() { Key = PermissionConstants.FUNCTION_USER_UPDATE, Description = "Cập nhật thông tin và vai trò của người dùng", ParentKey = PermissionConstants.MENU_USER, Module = PermissionConstants.MODULE_AUTH },
            new() { Key = PermissionConstants.FUNCTION_USER_DELETE, Description = "Xóa tài khoản người dùng", ParentKey = PermissionConstants.MENU_USER, Module = PermissionConstants.MODULE_AUTH },

            new() { Key = PermissionConstants.MENU_ROLE, Description = "Menu quản lý vai trò", ParentKey = PermissionConstants.MENU_AUTH, Module = PermissionConstants.MODULE_AUTH },
            new() { Key = PermissionConstants.FUNCTION_ROLE_VIEW, Description = "Xem danh sách và thông tin chi tiết vai trò", ParentKey = PermissionConstants.MENU_ROLE, Module = PermissionConstants.MODULE_AUTH },
            new() { Key = PermissionConstants.FUNCTION_ROLE_CREATE, Description = "Tạo mới vai trò", ParentKey = PermissionConstants.MENU_ROLE, Module = PermissionConstants.MODULE_AUTH },
            new() { Key = PermissionConstants.FUNCTION_ROLE_UPDATE, Description = "Cập nhật tên và mô tả vai trò", ParentKey = PermissionConstants.MENU_ROLE, Module = PermissionConstants.MODULE_AUTH },
            new() { Key = PermissionConstants.FUNCTION_ROLE_DELETE, Description = "Xóa vai trò khỏi hệ thống", ParentKey = PermissionConstants.MENU_ROLE, Module = PermissionConstants.MODULE_AUTH },
            new() { Key = PermissionConstants.FUNCTION_ROLE_ASSIGN_PERMISSION, Description = "Gán hoặc thu hồi quyền của vai trò", ParentKey = PermissionConstants.MENU_ROLE, Module = PermissionConstants.MODULE_AUTH },

            new() { Key = PermissionConstants.MENU_PERMISSION, Description = "Menu quản lý quyền", ParentKey = PermissionConstants.MENU_AUTH, Module = PermissionConstants.MODULE_AUTH },
            new() { Key = PermissionConstants.FUNCTION_PERMISSION_VIEW, Description = "Xem danh sách quyền trong hệ thống", ParentKey = PermissionConstants.MENU_PERMISSION, Module = PermissionConstants.MODULE_AUTH },

            new() { Key = PermissionConstants.MENU_CORE, Description = "Menu hệ thống", Module = PermissionConstants.MODULE_CORE },

            new() { Key = PermissionConstants.MENU_AUDIT_LOG, Description = "Menu nhật ký hệ thống", ParentKey = PermissionConstants.MENU_CORE, Module = PermissionConstants.MODULE_CORE },
            new() { Key = PermissionConstants.FUNCTION_AUDIT_LOG_VIEW, Description = "Xem danh sách và thông tin chi tiết nhật ký hệ thống", ParentKey = PermissionConstants.MENU_AUDIT_LOG, Module = PermissionConstants.MODULE_CORE },
        ];

        public static async Task SeedAsync(ShDbContext dbContext)
        {
            var defaultKeys = DefaultPermissions.Select(p => p.Key).ToHashSet();

            var existingPermissions = await dbContext.Permissions
                .ToDictionaryAsync(p => p.Key);

            var obsoletePermissions = existingPermissions.Values
                .Where(p => !defaultKeys.Contains(p.Key))
                .ToList();

            if (obsoletePermissions.Count > 0)
            {
                var obsoleteKeys = obsoletePermissions.Select(p => p.Key).ToList();
                var obsoleteRolePermissions = await dbContext.RolePermissions
                    .Where(rp => obsoleteKeys.Contains(rp.PermissionKey))
                    .ToListAsync();

                dbContext.RolePermissions.RemoveRange(obsoleteRolePermissions);
                dbContext.Permissions.RemoveRange(obsoletePermissions);
            }

            foreach (var permission in DefaultPermissions)
            {
                if (!existingPermissions.TryGetValue(permission.Key, out var existing))
                {
                    dbContext.Permissions.Add(new Permission
                    {
                        Key = permission.Key,
                        Description = permission.Description,
                        ParentKey = permission.ParentKey,
                        Module = permission.Module,
                        CreatedBy = "system",
                    });
                    continue;
                }

                if (existing.Description == permission.Description
                    && existing.ParentKey == permission.ParentKey
                    && existing.Module == permission.Module
                    && !existing.Deleted)
                {
                    continue;
                }

                existing.Description = permission.Description;
                existing.ParentKey = permission.ParentKey;
                existing.Module = permission.Module;
                existing.Deleted = false;
                existing.DeletedBy = null;
                existing.DeletedDate = null;
                existing.ModifiedBy = "system";
                existing.ModifiedDate = DateTime.Now;
            }

            if (dbContext.ChangeTracker.HasChanges())
            {
                var entries = dbContext.ChangeTracker.Entries<Permission>().ToList();
                dbContext.AuditLogs.Add(new AuditLog
                {
                    Action = AuditLogConstants.ACTION_SEED_PERMISSION,
                    ActionType = AuditLogConstants.ACTION_TYPE_UPDATE,
                    Content = "Đồng bộ danh sách quyền theo cấu hình hệ thống",
                    OldData = AuditLogUtils.ToJson(new
                    {
                        Deleted = entries.Where(e => e.State == EntityState.Deleted).Select(e => e.Entity.Key),
                    }),
                    NewData = AuditLogUtils.ToJson(new
                    {
                        Added = entries.Where(e => e.State == EntityState.Added).Select(e => e.Entity.Key),
                        Modified = entries.Where(e => e.State == EntityState.Modified).Select(e => e.Entity.Key),
                    }),
                    Source = nameof(SeedPermission),
                    CreatedDate = DateTimeUtils.GetVietnamTime(),
                });
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
