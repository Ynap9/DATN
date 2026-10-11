using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Auth.Entities;
using sh.be.domain.Module.Auth.Repositories;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Module.Auth.Repositories
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly ShDbContext _shDbContext;

        public PermissionRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public List<Permission> GetActive()
        {
            return _shDbContext.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Key)
                .ToList();
        }

        public Task<List<Permission>> GetActiveAsync()
        {
            return _shDbContext.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Key)
                .ToListAsync();
        }
    }
}
