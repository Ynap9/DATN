using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.shared.Constants.Auth
{
    public static class RoleConstants
    {
        public const string ROLE_SUPER_ADMIN = "SuperAdmin";
        public const string ROLE_ADMIN = "Admin";
        public const string ROLE_LEADER = "Leader";
        public const string ROLE_DATA = "Data";
        public const string ROLE_QA = "QA";
        public const string ROLE_USER = "User";

        public static readonly IReadOnlyList<string> EXCLUSIVE_STAFF_ROLES = [ROLE_DATA, ROLE_QA];
    }


}
