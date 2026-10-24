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

            Permission.Create(PermissionConstants.MENU_CONSTRUCTION, "Menu nghiệp vụ xây dựng", null, PermissionConstants.MODULE_CONSTRUCTION, null),

            Permission.Create(PermissionConstants.MENU_DOCUMENT_TYPE, "Menu quản lý loại văn bản", PermissionConstants.MENU_CONSTRUCTION, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_DOCUMENT_TYPE_VIEW, "Xem danh sách và chi tiết loại văn bản", PermissionConstants.MENU_DOCUMENT_TYPE, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_DOCUMENT_TYPE_CREATE, "Tạo mới loại văn bản", PermissionConstants.MENU_DOCUMENT_TYPE, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_DOCUMENT_TYPE_UPDATE, "Cập nhật loại văn bản", PermissionConstants.MENU_DOCUMENT_TYPE, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_DOCUMENT_TYPE_DELETE, "Xóa loại văn bản", PermissionConstants.MENU_DOCUMENT_TYPE, PermissionConstants.MODULE_CONSTRUCTION, null),

            Permission.Create(PermissionConstants.MENU_DOCUMENT_FIELD, "Menu quản lý trường của loại văn bản", PermissionConstants.MENU_CONSTRUCTION, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_DOCUMENT_FIELD_VIEW, "Xem danh sách và chi tiết trường của loại văn bản", PermissionConstants.MENU_DOCUMENT_FIELD, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_DOCUMENT_FIELD_CREATE, "Tạo mới trường của loại văn bản", PermissionConstants.MENU_DOCUMENT_FIELD, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_DOCUMENT_FIELD_UPDATE, "Cập nhật trường của loại văn bản", PermissionConstants.MENU_DOCUMENT_FIELD, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_DOCUMENT_FIELD_DELETE, "Xóa trường của loại văn bản", PermissionConstants.MENU_DOCUMENT_FIELD, PermissionConstants.MODULE_CONSTRUCTION, null),

            Permission.Create(PermissionConstants.MENU_PROJECT, "Menu quản lý dự án", PermissionConstants.MENU_CONSTRUCTION, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_PROJECT_VIEW, "Xem danh sách và chi tiết dự án", PermissionConstants.MENU_PROJECT, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_PROJECT_CREATE, "Tạo mới dự án", PermissionConstants.MENU_PROJECT, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_PROJECT_UPDATE, "Cập nhật dự án", PermissionConstants.MENU_PROJECT, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_PROJECT_DELETE, "Xóa dự án", PermissionConstants.MENU_PROJECT, PermissionConstants.MODULE_CONSTRUCTION, null),

            Permission.Create(PermissionConstants.MENU_PROJECT_CONFIG, "Menu cấu hình dự án", PermissionConstants.MENU_CONSTRUCTION, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_PROJECT_CONFIG_VIEW, "Xem cấu hình dự án", PermissionConstants.MENU_PROJECT_CONFIG, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_PROJECT_CONFIG_CREATE, "Tạo cấu hình dự án", PermissionConstants.MENU_PROJECT_CONFIG, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_PROJECT_CONFIG_UPDATE, "Cập nhật cấu hình dự án và xác thực tài khoản Google", PermissionConstants.MENU_PROJECT_CONFIG, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_PROJECT_CONFIG_DELETE, "Xóa cấu hình dự án", PermissionConstants.MENU_PROJECT_CONFIG, PermissionConstants.MODULE_CONSTRUCTION, null),

            Permission.Create(PermissionConstants.MENU_SUBMISSION, "Menu quản lý hồ sơ", PermissionConstants.MENU_CONSTRUCTION, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_SUBMISSION_VIEW, "Xem danh sách, chi tiết, lịch sử và file hồ sơ", PermissionConstants.MENU_SUBMISSION, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_SUBMISSION_IMPORT, "Import hồ sơ từ Google Sheet", PermissionConstants.MENU_SUBMISSION, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_SUBMISSION_CHECK, "Kiểm tra và sửa dữ liệu hồ sơ", PermissionConstants.MENU_SUBMISSION, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_SUBMISSION_QA, "Đánh giá QA hồ sơ", PermissionConstants.MENU_SUBMISSION, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_SUBMISSION_DELETE, "Xoá hồ sơ", PermissionConstants.MENU_SUBMISSION, PermissionConstants.MODULE_CONSTRUCTION, null),
            Permission.Create(PermissionConstants.FUNCTION_SUBMISSION_EXPORT, "Xuất Excel hồ sơ", PermissionConstants.MENU_SUBMISSION, PermissionConstants.MODULE_CONSTRUCTION, null),

            Permission.Create(PermissionConstants.MENU_DIGITAL_SIGNATURE, "Menu ký số", null, PermissionConstants.MODULE_DIGITAL_SIGNATURE, null),

            Permission.Create(PermissionConstants.MENU_TEMPLATE, "Menu quản lý mẫu chữ ký", PermissionConstants.MENU_DIGITAL_SIGNATURE, PermissionConstants.MODULE_DIGITAL_SIGNATURE, null),
            Permission.Create(PermissionConstants.FUNCTION_TEMPLATE_VIEW, "Xem danh sách và chi tiết mẫu chữ ký", PermissionConstants.MENU_TEMPLATE, PermissionConstants.MODULE_DIGITAL_SIGNATURE, null),
            Permission.Create(PermissionConstants.FUNCTION_TEMPLATE_CREATE, "Tạo mới mẫu chữ ký", PermissionConstants.MENU_TEMPLATE, PermissionConstants.MODULE_DIGITAL_SIGNATURE, null),
            Permission.Create(PermissionConstants.FUNCTION_TEMPLATE_UPDATE, "Cập nhật mẫu chữ ký và cấu hình ký", PermissionConstants.MENU_TEMPLATE, PermissionConstants.MODULE_DIGITAL_SIGNATURE, null),
            Permission.Create(PermissionConstants.FUNCTION_TEMPLATE_DELETE, "Xóa mẫu chữ ký", PermissionConstants.MENU_TEMPLATE, PermissionConstants.MODULE_DIGITAL_SIGNATURE, null),

            Permission.Create(PermissionConstants.MENU_SIGNING_BATCH, "Menu lô ký số", PermissionConstants.MENU_DIGITAL_SIGNATURE, PermissionConstants.MODULE_DIGITAL_SIGNATURE, null),
            Permission.Create(PermissionConstants.FUNCTION_SIGNING_BATCH_VIEW, "Xem lô ký và danh sách file", PermissionConstants.MENU_SIGNING_BATCH, PermissionConstants.MODULE_DIGITAL_SIGNATURE, null),
            Permission.Create(PermissionConstants.FUNCTION_SIGNING_BATCH_CREATE, "Tạo lô ký và tải file lên", PermissionConstants.MENU_SIGNING_BATCH, PermissionConstants.MODULE_DIGITAL_SIGNATURE, null),
            Permission.Create(PermissionConstants.FUNCTION_SIGNING_BATCH_SIGN, "Ký số lô (mở / đóng phiên, bắt đầu, tạm dừng, huỷ)", PermissionConstants.MENU_SIGNING_BATCH, PermissionConstants.MODULE_DIGITAL_SIGNATURE, null),
            Permission.Create(PermissionConstants.FUNCTION_SIGNING_BATCH_DELETE, "Xóa file khỏi lô ký", PermissionConstants.MENU_SIGNING_BATCH, PermissionConstants.MODULE_DIGITAL_SIGNATURE, null),

            Permission.Create(PermissionConstants.MENU_PACKAGING, "Menu đóng gói", null, PermissionConstants.MODULE_PACKAGING, null),

            Permission.Create(PermissionConstants.MENU_PACKAGE_CONFIG, "Menu cấu hình metadata đóng gói", PermissionConstants.MENU_PACKAGING, PermissionConstants.MODULE_PACKAGING, null),
            Permission.Create(PermissionConstants.FUNCTION_PACKAGE_CONFIG_VIEW, "Xem cấu hình metadata và tải Excel mẫu", PermissionConstants.MENU_PACKAGE_CONFIG, PermissionConstants.MODULE_PACKAGING, null),
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
