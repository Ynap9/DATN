using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Auth.Dtos.User
{
    public class ViewUserDto
    {
        public string Id { get; set; } = String.Empty;
        public string UserName { get; set; } = String.Empty;
        public string FullName { get; set; } = String.Empty;
        public string Email { get; set; } = String.Empty;
        public List<UserRolePaging> Roles { get; set; } = new List<UserRolePaging>();
    }

    public class UserRolePaging
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
    }
}
