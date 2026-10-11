using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Auth.Entities;
using sh.be.domain.Module.Auth.Repositories;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Module.Auth.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly ShDbContext _shDbContext;

        public RoleRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public Role? FindById(int id)
        {
            return _shDbContext.Roles.FirstOrDefault(r => r.Id == id);
        }

        public Task<Role?> FindByIdAsync(int id)
        {
            return _shDbContext.Roles.FirstOrDefaultAsync(r => r.Id == id);
        }

        public bool Exists(int id)
        {
            return _shDbContext.Roles.Any(r => r.Id == id);
        }

        public Task<int> CountActiveAsync(List<int> ids)
        {
            return _shDbContext.Roles.CountAsync(r => ids.Contains(r.Id));
        }

        public void Add(Role role)
        {
            _shDbContext.Roles.Add(role);
        }

        public void Update(Role role)
        {
            _shDbContext.Roles.Update(role);
        }

        public List<string> GetPermissionKeys(int roleId)
        {
            return _shDbContext.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionKey)
                .ToList();
        }

        public Task<List<string>> GetPermissionKeysAsync(int roleId)
        {
            return _shDbContext.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionKey)
                .ToListAsync();
        }

        public async Task ReplacePermissionsAsync(int roleId, List<RolePermission> rolePermissions)
        {
            await _shDbContext.RolePermissions
                .IgnoreQueryFilters()
                .Where(rp => rp.RoleId == roleId)
                .ExecuteDeleteAsync();

            _shDbContext.RolePermissions.AddRange(rolePermissions);
        }

        public Task<List<string>> GetRoleNamesByUserIdAsync(string userId)
        {
            return (from ur in _shDbContext.UserRoles
                    join r in _shDbContext.Roles on ur.RoleId equals r.Id
                    where ur.UserId == userId
                    select r.Name).ToListAsync();
        }

        public Task<List<string>> GetPermissionKeysByUserIdAsync(string userId)
        {
            return (from ur in _shDbContext.UserRoles
                    join r in _shDbContext.Roles on ur.RoleId equals r.Id
                    join rp in _shDbContext.RolePermissions on ur.RoleId equals rp.RoleId
                    where ur.UserId == userId
                    select rp.PermissionKey).Distinct().ToListAsync();
        }
    }
}
