using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.domain.Module.Auth.Entities
{
    public class AppUser: IdentityUser
    {
        [MaxLength(250)]
        public string FullName { get; private set; } = String.Empty;
        [MaxLength(450)]
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }

        public static AppUser Create(string userName, string fullName, string email, string? phoneNumber, string? createdBy = null, DateTime? createdDate = null)
        {
            return new AppUser
            {
                UserName = userName,
                FullName = fullName,
                Email = email,
                PhoneNumber = phoneNumber,
                EmailConfirmed = true,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void UpdateProfile(string? userName, string? fullName, string? email, string? phoneNumber)
        {
            if (!string.IsNullOrWhiteSpace(userName))
            {
                UserName = userName;
            }
            if (!string.IsNullOrWhiteSpace(email))
            {
                Email = email;
            }
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                FullName = fullName;
            }
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                PhoneNumber = phoneNumber;
            }
        }

        public List<UserRole> AssignRoles(IEnumerable<int> roleIds, string? assignedBy, DateTime? assignedDate)
        {
            return roleIds
                .Select(roleId => UserRole.Create(Id, roleId, assignedBy, assignedDate))
                .ToList();
        }
    }
}
