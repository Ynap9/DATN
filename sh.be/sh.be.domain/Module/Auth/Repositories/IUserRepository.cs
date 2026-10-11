using sh.be.domain.Module.Auth.Entities;

namespace sh.be.domain.Module.Auth.Repositories
{
    public interface IUserRepository
    {
        Task<AppUser?> FindByIdAsync(string id);
        Task<AppUser?> FindByNameAsync(string userName);
        Task<AppUser?> FindByEmailAsync(string email);
        Task CreateAsync(AppUser user, string password);
        Task UpdateAsync(AppUser user);
        Task DeleteAsync(AppUser user);
        Task<bool> CheckPasswordAsync(AppUser user, string password);
        Task ChangePasswordAsync(AppUser user, string currentPassword, string newPassword);
        Task<List<UserRole>> GetUserRolesAsync(string userId);
        Task<List<int>> GetActiveRoleIdsAsync(string userId);
        void AddUserRoles(IEnumerable<UserRole> userRoles);
        Task DeleteUserRolesAsync(string userId);
    }
}
