using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Auth.Entities;
using sh.be.domain.Module.Core.Entities;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Core;
using sh.be.shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.infrastructure.data.Persistence.Seeder
{
    public static class SeedUser
    {
        public static async Task SeedAsync(
            ShDbContext dbContext,
            UserManager<AppUser> userManager)
        {
            var adminRoleName = "SuperAdmin";
            var adminRole = await dbContext.Roles
                .FirstOrDefaultAsync(r => r.Name == adminRoleName);
            if (adminRole == null)
            {
                adminRole = Role.Create(adminRoleName, "Super Administrator", "system", null);
                dbContext.Roles.Add(adminRole);
                dbContext.AuditLogs.Add(AuditLog.Create(
                    null,
                    AuditLogConstants.ACTION_SEED_ROLE,
                    AuditLogConstants.ACTION_TYPE_CREATE,
                    $"Khởi tạo vai trò mặc định: {adminRole.Name}",
                    null,
                    new { adminRole.Name, adminRole.Description },
                    nameof(SeedUser),
                    null,
                    DateTimeUtils.GetVietnamTime()));
                await dbContext.SaveChangesAsync();
            }

            var adminEmail = "admin@example.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = AppUser.Create("admin", "Super Administrator", adminEmail, null);

                dbContext.AuditLogs.Add(AuditLog.Create(
                    null,
                    AuditLogConstants.ACTION_SEED_USER,
                    AuditLogConstants.ACTION_TYPE_CREATE,
                    $"Khởi tạo tài khoản quản trị mặc định: {adminUser.UserName}",
                    null,
                    new { adminUser.Id, adminUser.UserName, adminUser.FullName, adminUser.Email },
                    nameof(SeedUser),
                    null,
                    DateTimeUtils.GetVietnamTime()));
                var result = await userManager.CreateAsync(adminUser, "123456Aa@");
                if (!result.Succeeded)
                {
                    throw new Exception("Failed to create admin: " +
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            var hasRole = await dbContext.UserRoles
                .IgnoreQueryFilters()
                .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id);
            if (!hasRole)
            {
                dbContext.UserRoles.Add(UserRole.Create(adminUser.Id, adminRole.Id, "system", null));
                dbContext.AuditLogs.Add(AuditLog.Create(
                    null,
                    AuditLogConstants.ACTION_USER_UPDATE_ROLE,
                    AuditLogConstants.ACTION_TYPE_CREATE,
                    $"Gán vai trò {adminRole.Name} cho tài khoản {adminUser.UserName}",
                    null,
                    new { UserId = adminUser.Id, RoleId = adminRole.Id },
                    nameof(SeedUser),
                    null,
                    DateTimeUtils.GetVietnamTime()));
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
