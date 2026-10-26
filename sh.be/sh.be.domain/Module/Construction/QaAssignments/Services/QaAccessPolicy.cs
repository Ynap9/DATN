using sh.be.domain.Module.Auth.Services;
using sh.be.shared.Constants.Auth;

namespace sh.be.domain.Module.Construction.QaAssignments.Services
{
    public static class QaAccessPolicy
    {
        public static bool IsAdmin(IReadOnlyCollection<string> roleNames)
        {
            return PermissionGrantPolicy.IsSuperAdmin(roleNames) || roleNames.Contains(RoleConstants.ROLE_ADMIN);
        }
    }
}
