using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Auth.Dtos.Role
{
    public class UpdateRolePermissionDto
    {
        public int RoleId { get; set; }
        public List<UpdateRolePermissionNodeDto> Permissions { get; set; } = new List<UpdateRolePermissionNodeDto>();
    }

    public class UpdateRolePermissionNodeDto
    {
        public string Key { get; set; } = String.Empty;
        public bool IsChecked { get; set; }
        public List<UpdateRolePermissionNodeDto> Children { get; set; } = new List<UpdateRolePermissionNodeDto>();
    }
}
