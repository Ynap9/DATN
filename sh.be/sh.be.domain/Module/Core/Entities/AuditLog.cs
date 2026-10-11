using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using sh.be.shared.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.domain.Module.Core.Entities
{
    [Table(nameof(AuditLog), Schema = DbSchemas.Core)]
    [Index(nameof(CreatedDate), Name = $"IX_{nameof(AuditLog)}_{nameof(CreatedDate)}")]
    [Index(nameof(UserId), Name = $"IX_{nameof(AuditLog)}_{nameof(UserId)}")]
    [Index(nameof(ActionType), Name = $"IX_{nameof(AuditLog)}_{nameof(ActionType)}")]
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; private set; }
        [MaxLength(450)]
        public string? UserId { get; private set; }
        [MaxLength(250)]
        public string Action { get; private set; } = String.Empty;
        [MaxLength(50)]
        public string ActionType { get; private set; } = String.Empty;
        public string Content { get; private set; } = String.Empty;
        public string? OldData { get; private set; }
        public string? NewData { get; private set; }
        [MaxLength(500)]
        public string Source { get; private set; } = String.Empty;
        [MaxLength(250)]
        public string? ContentType { get; private set; }
        public DateTime CreatedDate { get; private set; }

        private AuditLog()
        {
        }

        public static AuditLog Create(string? userId, string action, string actionType, string content, object? oldData, object? newData,
            string source, string? contentType, DateTime createdDate)
        {
            return new AuditLog
            {
                UserId = userId,
                Action = action,
                ActionType = actionType,
                Content = content,
                OldData = AuditLogUtils.ToJson(oldData),
                NewData = AuditLogUtils.ToJson(newData),
                Source = source,
                ContentType = contentType,
                CreatedDate = createdDate,
            };
        }
    }
}
