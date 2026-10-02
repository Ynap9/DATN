using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Auth;
using sh.be.domain.Module.Core;
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
            new() { Name = RoleConstants.ROLE_ADMIN, Description = "Admin" },
            new() { Name = RoleConstants.ROLE_LEADER, Description = "Quản lý" },
            new() { Name = RoleConstants.ROLE_DATA, Description = "Nhân viên nhập liệu" },
            new() { Name = RoleConstants.ROLE_QA, Description = "Nhân viên QA" },
            new() { Name = RoleConstants.ROLE_USER, Description = "Người dùng" },
        ];

        public static async Task SeedAsync(ShDbContext dbContext)
        {
            var existingNames = await dbContext.Roles
                .Where(r => !r.Deleted)
                .Select(r => r.Name)
                .ToListAsync();

            var newRoles = DefaultRoles
                .Where(r => !existingNames.Contains(r.Name))
                .Select(r => new Role
                {
                    Name = r.Name,
                    Description = r.Description,
                    IsSystem = 1,
                    CreatedBy = "system",
                })
                .ToList();

            if (newRoles.Count == 0)
            {
                return;
            }

            dbContext.Roles.AddRange(newRoles);
            dbContext.AuditLogs.Add(new AuditLog
            {
                Action = AuditLogConstants.ACTION_SEED_ROLE,
                ActionType = AuditLogConstants.ACTION_TYPE_CREATE,
                Content = $"Khởi tạo vai trò mặc định: {string.Join(", ", newRoles.Select(r => r.Name))}",
                NewData = AuditLogUtils.ToJson(newRoles.Select(r => new { r.Name, r.Description })),
                Source = nameof(SeedRole),
                CreatedDate = DateTimeUtils.GetVietnamTime(),
            });
            await dbContext.SaveChangesAsync();
        }
    }
}
