using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.domain.Module.Core
{
    [Table(nameof(AuditLog), Schema = DbSchemas.Core)]
    [Index(nameof(CreatedDate), Name = $"IX_{nameof(AuditLog)}_{nameof(CreatedDate)}")]
    [Index(nameof(UserId), Name = $"IX_{nameof(AuditLog)}_{nameof(UserId)}")]
    [Index(nameof(ActionType), Name = $"IX_{nameof(AuditLog)}_{nameof(ActionType)}")]
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        [MaxLength(450)]
        public string? UserId { get; set; }
        [MaxLength(250)]
        public string Action { get; set; } = String.Empty;
        [MaxLength(50)]
        public string ActionType { get; set; } = String.Empty;
        public string Content { get; set; } = String.Empty;
        public string? OldData { get; set; }
        public string? NewData { get; set; }
        [MaxLength(500)]
        public string Source { get; set; } = String.Empty;
        [MaxLength(250)]
        public string? ContentType { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
