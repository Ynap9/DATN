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
    [Table(nameof(UserRole), Schema = DbSchemas.Auth)]
    public class UserRole: ISoftDeleted
    {
        public string UserId { get; private set; } = String.Empty;
        public int RoleId { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private UserRole()
        {
        }

        public static UserRole Create(string userId, int roleId, string? createdBy, DateTime? createdDate)
        {
            return new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void Delete(string? deletedBy, DateTime deletedDate)
        {
            Deleted = true;
            DeletedDate = deletedDate;
            DeletedBy = deletedBy;
        }
    }
}
