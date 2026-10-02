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

namespace sh.be.domain.Module.Auth
{
    [Table(nameof(Permission), Schema = DbSchemas.Auth)]
    [Index(nameof(Key), IsUnique = true, Name = $"IX_{nameof(Permission)}")]
    public class Permission : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        //public int Id { get; set; }
        public string Key { get; set; } = String.Empty;
        public string Description { get; set; } = String.Empty;
        public string? ParentKey { get; set; } 
        public string Module { get; set; } = String.Empty;
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public bool Deleted { get; set; }
        public string? DeletedBy { get; set; }

    }
}
