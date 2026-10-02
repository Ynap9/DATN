using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Auth.Dtos.User
{
    public class ViewMeDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<UserRoleDto> Roles { get; set; } = new List<UserRoleDto>();
        //public List<UserPermissionDto> Permissions { get; set; } = new List<UserPermissionDto>();
    }
    public class UserRoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public string Description { get; set; } = String.Empty;
        public List<UserPermissionDto> Permissions { get; set; } = new List<UserPermissionDto>();

    }
    public class UserPermissionDto
    {
        public string Key { get; set; } = String.Empty;
        public string Description { get; set; } = String.Empty;
        public string? ParentKey { get; set; }
        public string Module { get; set; } = String.Empty;
    }
}
