using sh.be.domain.Module.Auth.Entities;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Utils;

namespace sh.be.domain.Module.Auth.Services
{
    public static class PermissionDomainService
    {
        public static HashSet<string> ResolveGrantedKeys(List<Permission> permissions, IEnumerable<string> checkedKeys)
        {
            var checkedKeySet = checkedKeys.ToHashSet();
            var nodes = permissions
                .Select(p => new PermissionNode
                {
                    Key = p.Key,
                    ParentKey = p.ParentKey,
                    IsChecked = checkedKeySet.Contains(p.Key),
                }).ToList();

            var tree = TreeUtils.BuildTree(nodes, n => n.Key, n => n.ParentKey, n => n.Children);
            TreeUtils.SyncChecked(tree, n => n.Children, n => n.IsChecked, (n, isChecked) => n.IsChecked = isChecked);

            return nodes
                .Where(n => n.IsChecked)
                .Select(n => n.Key)
                .ToHashSet();
        }

        public static bool IsSuperAdmin(List<string> roleNames)
        {
            return roleNames.Contains(RoleConstants.ROLE_SUPER_ADMIN);
        }

        public static bool IsAdmin(List<string> roleNames)
        {
            return IsSuperAdmin(roleNames) || roleNames.Contains(RoleConstants.ROLE_ADMIN);
        }

        public static bool HasAnyPermission(List<Permission> permissions, List<string> grantedKeys, string[] requiredKeys)
        {
            var resolvedKeys = ResolveGrantedKeys(permissions, grantedKeys);
            return requiredKeys.Any(resolvedKeys.Contains);
        }

        private class PermissionNode
        {
            public string Key { get; set; } = String.Empty;
            public string? ParentKey { get; set; }
            public bool IsChecked { get; set; }
            public List<PermissionNode> Children { get; set; } = new List<PermissionNode>();
        }
    }
}
