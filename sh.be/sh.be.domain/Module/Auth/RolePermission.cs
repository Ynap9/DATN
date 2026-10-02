using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.domain.Module.Auth
{
    [Table(nameof(RolePermission), Schema = DbSchemas.Auth)]
    public class RolePermission : ISoftDeleted
    {
        public int RoleId { get; set; }
        public string PermissionParentKey { get; set; } = String.Empty;
        public string PermissionKey { get; set; } = String.Empty;
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public bool Deleted { get; set; }
        public string? DeletedBy { get; set; }
    }
}
