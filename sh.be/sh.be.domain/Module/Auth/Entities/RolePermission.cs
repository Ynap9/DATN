using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.domain.Module.Auth.Entities
{
    [Table(nameof(RolePermission), Schema = DbSchemas.Auth)]
    public class RolePermission : ISoftDeleted
    {
        public int RoleId { get; private set; }
        public string PermissionParentKey { get; private set; } = String.Empty;
        public string PermissionKey { get; private set; } = String.Empty;
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private RolePermission()
        {
        }

        public static RolePermission Create(int roleId, string permissionKey, string permissionParentKey, string? createdBy, DateTime? createdDate)
        {
            return new RolePermission
            {
                RoleId = roleId,
                PermissionParentKey = permissionParentKey,
                PermissionKey = permissionKey,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }
    }
}
