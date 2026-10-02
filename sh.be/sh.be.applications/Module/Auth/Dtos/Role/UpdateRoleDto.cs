using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Auth.Dtos.Role
{
    public class UpdateRoleDto
    {
        public string Name { get; set; } = String.Empty;
        public string Description { get; set; } = String.Empty;
    }
}
