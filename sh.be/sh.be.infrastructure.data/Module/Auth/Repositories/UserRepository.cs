using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Auth.Entities;
using sh.be.domain.Module.Auth.Repositories;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.infrastructure.data.Module.Auth.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ShDbContext _shDbContext;
        private readonly UserManager<AppUser> _userManager;

        public UserRepository(ShDbContext shDbContext, UserManager<AppUser> userManager)
        {
            _shDbContext = shDbContext;
            _userManager = userManager;
        }

        public Task<AppUser?> FindByIdAsync(string id)
        {
            return _userManager.FindByIdAsync(id);
        }

        public Task<AppUser?> FindByNameAsync(string userName)
        {
            return _userManager.FindByNameAsync(userName);
        }

        public Task<AppUser?> FindByEmailAsync(string email)
        {
            return _userManager.FindByEmailAsync(email);
        }

        public async Task CreateAsync(AppUser user, string password)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorCreateUser, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        public async Task UpdateAsync(AppUser user)
        {
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorUpdateUser, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        public async Task DeleteAsync(AppUser user)
        {
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorDeleteUser, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        public Task<bool> CheckPasswordAsync(AppUser user, string password)
        {
            return _userManager.CheckPasswordAsync(user, password);
        }

        public async Task ChangePasswordAsync(AppUser user, string currentPassword, string newPassword)
        {
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorChangePassword, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        public Task<List<UserRole>> GetUserRolesAsync(string userId)
        {
            return _shDbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync();
        }

        public Task<List<int>> GetActiveRoleIdsAsync(string userId)
        {
            return _shDbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();
        }

        public void AddUserRoles(IEnumerable<UserRole> userRoles)
        {
            _shDbContext.UserRoles.AddRange(userRoles);
        }

        public Task DeleteUserRolesAsync(string userId)
        {
            return _shDbContext.UserRoles
                .IgnoreQueryFilters()
                .Where(ur => ur.UserId == userId)
                .ExecuteDeleteAsync();
        }
    }
}
