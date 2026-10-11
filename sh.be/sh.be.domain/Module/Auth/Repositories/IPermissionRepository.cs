using sh.be.domain.Module.Auth.Entities;

namespace sh.be.domain.Module.Auth.Repositories
{
    public interface IPermissionRepository
    {
        List<Permission> GetActive();
        Task<List<Permission>> GetActiveAsync();
    }
}
