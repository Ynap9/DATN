using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Auth.Dtos.User
{
    public class UpdateRoleUserDto
    {
        public string UserId {  get; set; } = String.Empty;
        public List<int>RoleId { get; set; } =new List<int>();
    }
}
