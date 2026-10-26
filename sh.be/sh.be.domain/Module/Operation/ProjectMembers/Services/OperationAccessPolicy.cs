using sh.be.domain.Module.Auth.Services;
using sh.be.shared.Constants.Auth;

namespace sh.be.domain.Module.Operation.ProjectMembers.Services
{
    public static class OperationAccessPolicy
    {
        public static bool IsAdmin(IEnumerable<string> roleNames)
        {
            return PermissionGrantPolicy.IsSuperAdmin(roleNames) || roleNames.Contains(RoleConstants.ROLE_ADMIN);
        }
    }
}
