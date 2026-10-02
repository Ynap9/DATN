using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Core.Dtos.AuditLog
{
    public class ViewAuditLogDto
    {
        public long Id { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string Action { get; set; } = String.Empty;
        public string ActionType { get; set; } = String.Empty;
        //public string Content { get; set; } = String.Empty;
        public string Source { get; set; } = String.Empty;
        public string? ContentType { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
