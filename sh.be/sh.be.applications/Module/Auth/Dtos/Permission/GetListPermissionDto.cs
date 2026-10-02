using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Auth.Dtos.Permission
{
    public class GetListPermissionDto
    {
        public string ParentKey { get; set; } = String.Empty;
        public string Key { get; set; } = String.Empty;
    }
}
