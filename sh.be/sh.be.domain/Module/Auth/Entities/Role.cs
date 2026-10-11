using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sh.be.shared.Constants.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sh.be.shared.Interfaces;
using sh.be.domain.Module.Auth.Services;

namespace sh.be.domain.Module.Auth.Entities
{
    [Table(nameof(Role), Schema = DbSchemas.Auth)]
    public class Role: ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public string Name { get; private set; } = String.Empty;
        public string Description { get; private set; } = String.Empty;
        public int IsSystem { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private Role()
        {
        }

        public static Role Create(string name, string description, string? createdBy, DateTime? createdDate, int id = 0)
        {
            return new Role
            {
                Id = id,
                Name = name,
                Description = description,
                IsSystem = 1,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void Update(string name, string description, string? modifiedBy, DateTime modifiedDate)
        {
            Name = name;
            Description = description;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Delete(string? deletedBy, DateTime deletedDate)
        {
            Deleted = true;
            DeletedDate = deletedDate;
            DeletedBy = deletedBy;
        }

        public List<RolePermission> AssignPermissions(IEnumerable<Permission> permissions, IEnumerable<string> checkedKeys, string? assignedBy, DateTime assignedDate)
        {
            var permissionList = permissions.ToList();
            var grantedKeys = PermissionGrantPolicy.ResolveGrantedKeys(permissionList, checkedKeys);
            return permissionList
                .Where(p => grantedKeys.Contains(p.Key))
                .Select(p => RolePermission.Create(Id, p.Key, p.ParentKey ?? String.Empty, assignedBy, assignedDate))
                .ToList();
        }
    }
}
