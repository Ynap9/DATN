using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Auth.Dtos.User
{
    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = String.Empty;
        public string NewPassword { get; set; } = String.Empty;
        public string NewPasswordConfirm { get; set; } = String.Empty;
    }
}
