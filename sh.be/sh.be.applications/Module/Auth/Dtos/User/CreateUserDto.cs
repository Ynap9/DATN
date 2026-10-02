using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Auth.Dtos.User
{
    public class CreateUserDto
    {
        //public Guid Id { get; set; }
        public string UserName { get; set; } = String.Empty;
        public string FullName { get; set; } = String.Empty;
        public string Email { get; set; } = String.Empty;
        public string? PhoneNumber { get; set; } = String.Empty;
        public string? Password {  get; set; } = String.Empty;
        public string? PasswordConfirm {  get; set; } = String.Empty;
        public bool IsRandomPassword { get; set; }
        public List<int> RoleId { get; set; } = new List<int> { };

    }
    
}
