using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Auth.Dtos.Role
{
    public class GetRolePermissionTreeDto
    {
        public string Key { get; set; } = String.Empty;
        public string? ParentKey { get; set; }
        public string Description { get; set; } = String.Empty;
        public string Module { get; set; } = String.Empty;
        public bool IsChecked { get; set; }
        public List<GetRolePermissionTreeDto> Children { get; set; } = new List<GetRolePermissionTreeDto>();
    }
}
