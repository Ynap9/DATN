using sh.be.domain.Module.Auth.Entities;

namespace sh.be.domain.Module.Auth.Repositories
{
    public interface IRoleRepository
    {
        Role? FindById(int id);
        Task<Role?> FindByIdAsync(int id);
        bool Exists(int id);
        Task<int> CountActiveAsync(List<int> ids);
        Task<List<string>> GetNamesByIdsAsync(List<int> ids);
        void Add(Role role);
        void Update(Role role);
        List<string> GetPermissionKeys(int roleId);
        Task<List<string>> GetPermissionKeysAsync(int roleId);
        Task ReplacePermissionsAsync(int roleId, List<RolePermission> rolePermissions);
        Task<List<string>> GetRoleNamesByUserIdAsync(string userId);
        Task<List<string>> GetPermissionKeysByUserIdAsync(string userId);
    }
}
