using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Auth.Entities;
using sh.be.domain.Module.Core.Entities;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Core;
using sh.be.shared.Utils;

namespace sh.be.infrastructure.data.Persistence.Seeder
{
    public static class SeedRole
    {
        private static readonly Role[] DefaultRoles =
        [
            Role.Create(RoleConstants.ROLE_ADMIN, "Admin", null, null),
            Role.Create(RoleConstants.ROLE_LEADER, "Quản lý", null, null),
            Role.Create(RoleConstants.ROLE_DATA, "Nhân viên nhập liệu", null, null),
            Role.Create(RoleConstants.ROLE_QA, "Nhân viên QA", null, null),
            Role.Create(RoleConstants.ROLE_USER, "Người dùng", null, null),
        ];

        public static async Task SeedAsync(ShDbContext dbContext)
        {
            var existingNames = await dbContext.Roles
                .Select(r => r.Name)
                .ToListAsync();

            var newRoles = DefaultRoles
                .Where(r => !existingNames.Contains(r.Name))
                .Select(r => Role.Create(r.Name, r.Description, "system", null))
                .ToList();

            if (newRoles.Count == 0)
            {
                return;
            }

            dbContext.Roles.AddRange(newRoles);
            dbContext.AuditLogs.Add(AuditLog.Create(
                null,
                AuditLogConstants.ACTION_SEED_ROLE,
                AuditLogConstants.ACTION_TYPE_CREATE,
                $"Khởi tạo vai trò mặc định: {string.Join(", ", newRoles.Select(r => r.Name))}",
                null,
                newRoles.Select(r => new { r.Name, r.Description }),
                nameof(SeedRole),
                null,
                DateTimeUtils.GetVietnamTime()));
            await dbContext.SaveChangesAsync();
        }
    }
}
