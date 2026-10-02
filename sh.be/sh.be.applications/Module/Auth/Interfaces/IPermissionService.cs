using sh.be.applications.Module.Auth.Dtos.Permission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Auth.Interfaces
{
    public interface IPermissionService
    {
        List<GetListPermissionDto> GetListPermission();
    }
}
