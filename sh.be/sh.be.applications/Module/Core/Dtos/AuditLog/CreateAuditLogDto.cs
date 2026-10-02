using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Core.Dtos.AuditLog
{
    public class CreateAuditLogDto
    {
        public string? UserId { get; set; }
        public string Action { get; set; } = String.Empty;
        public string ActionType { get; set; } = String.Empty;
        public string Content { get; set; } = String.Empty;
        public object? OldData { get; set; }
        public object? NewData { get; set; }
    }
}
