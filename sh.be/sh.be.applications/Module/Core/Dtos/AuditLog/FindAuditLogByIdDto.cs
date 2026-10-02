using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Core.Dtos.AuditLog
{
    public class FindAuditLogByIdDto : ViewAuditLogDto
    {
        public string Content { get; set; } = String.Empty;
        public string? OldData { get; set; }
        public string? NewData { get; set; }
    }
}
