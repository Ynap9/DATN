using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Auth;
using sh.be.domain.Module.Core;
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
                .FirstOrDefaultAsync(r => r.Name == adminRoleName && !r.Deleted);
            if (adminRole == null)
            {
                adminRole = new Role
                {
                    Name = adminRoleName,
                    Description = "Super Administrator",
                    IsSystem = 1,
                    CreatedBy = "system",
                };
                dbContext.Roles.Add(adminRole);
                dbContext.AuditLogs.Add(new AuditLog
                {
                    Action = AuditLogConstants.ACTION_SEED_ROLE,
                    ActionType = AuditLogConstants.ACTION_TYPE_CREATE,
                    Content = $"Khởi tạo vai trò mặc định: {adminRole.Name}",
                    NewData = AuditLogUtils.ToJson(new { adminRole.Name, adminRole.Description }),
                    Source = nameof(SeedUser),
                    CreatedDate = DateTimeUtils.GetVietnamTime(),
                });
                await dbContext.SaveChangesAsync();
            }

            var adminEmail = "admin@example.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new AppUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Super Administrator",
                };

                dbContext.AuditLogs.Add(new AuditLog
                {
                    Action = AuditLogConstants.ACTION_SEED_USER,
                    ActionType = AuditLogConstants.ACTION_TYPE_CREATE,
                    Content = $"Khởi tạo tài khoản quản trị mặc định: {adminUser.UserName}",
                    NewData = AuditLogUtils.ToJson(new { adminUser.Id, adminUser.UserName, adminUser.FullName, adminUser.Email }),
                    Source = nameof(SeedUser),
                    CreatedDate = DateTimeUtils.GetVietnamTime(),
                });
                var result = await userManager.CreateAsync(adminUser, "123456Aa@");
                if (!result.Succeeded)
                {
                    throw new Exception("Failed to create admin: " +
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            var hasRole = await dbContext.UserRoles
                .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id);
            if (!hasRole)
            {
                dbContext.UserRoles.Add(new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id,
                    CreatedBy = "system",
                });
                dbContext.AuditLogs.Add(new AuditLog
                {
                    Action = AuditLogConstants.ACTION_USER_UPDATE_ROLE,
                    ActionType = AuditLogConstants.ACTION_TYPE_CREATE,
                    Content = $"Gán vai trò {adminRole.Name} cho tài khoản {adminUser.UserName}",
                    NewData = AuditLogUtils.ToJson(new { UserId = adminUser.Id, RoleId = adminRole.Id }),
                    Source = nameof(SeedUser),
                    CreatedDate = DateTimeUtils.GetVietnamTime(),
                });
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
