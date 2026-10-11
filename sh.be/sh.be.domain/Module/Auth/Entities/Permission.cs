using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.domain.Module.Auth.Entities
{
    [Table(nameof(Permission), Schema = DbSchemas.Auth)]
    [Index(nameof(Key), IsUnique = true, Name = $"IX_{nameof(Permission)}")]
    public class Permission : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        //public int Id { get; set; }
        public string Key { get; private set; } = String.Empty;
        public string Description { get; private set; } = String.Empty;
        public string? ParentKey { get; private set; }
        public string Module { get; private set; } = String.Empty;
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private Permission()
        {
        }

        public static Permission Create(string key, string description, string? parentKey, string module, string? createdBy)
        {
            return new Permission
            {
                Key = key,
                Description = description,
                ParentKey = parentKey,
                Module = module,
                CreatedBy = createdBy,
            };
        }

        public bool Sync(string description, string? parentKey, string module, string? modifiedBy, DateTime modifiedDate)
        {
            if (Description == description
                && ParentKey == parentKey
                && Module == module
                && !Deleted)
            {
                return false;
            }

            Description = description;
            ParentKey = parentKey;
            Module = module;
            Deleted = false;
            DeletedBy = null;
            DeletedDate = null;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
            return true;
        }
    }
}
