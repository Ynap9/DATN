using sh.be.domain.Module.Auth.Repositories;
using sh.be.shared.Constants.Auth;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Auth.Services
{
    public class RoleDomainService
    {
        private readonly IRoleRepository _roleRepository;

        public RoleDomainService(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public void EnsureNotExists(int id)
        {
            if (_roleRepository.Exists(id))
            {
                throw new UserFriendlyException(ErrorCodes.Found, "Role đã tồn tại");
            }
        }

        public async Task EnsureAllExistAsync(List<int> roleIds)
        {
            var existingRoleCount = await _roleRepository.CountActiveAsync(roleIds);
            if (roleIds.Count == 0 || existingRoleCount != roleIds.Count)
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorRoleNotFound);
            }
        }

        public async Task EnsureCompatibleAsync(List<int> roleIds)
        {
            var roleNames = await _roleRepository.GetNamesByIdsAsync(roleIds);
            if (RoleConstants.EXCLUSIVE_STAFF_ROLES.Count(roleNames.Contains) >= 2)
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorConflictingRoles);
            }
        }
    }
}
