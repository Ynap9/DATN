using Microsoft.AspNetCore.Mvc;
using sh.be.shared.HttpRequest.BaseRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Core.Dtos.AuditLog
{
    public class FindPagingAuditLogDto : BaseRequestPagingDto
    {
        [FromQuery(Name = "userId")]
        public string? UserId { get; set; }
        [FromQuery(Name = "actionType")]
        public string? ActionType { get; set; }
        [FromQuery(Name = "source")]
        public string? Source { get; set; }
        [FromQuery(Name = "fromDate")]
        public DateTime? FromDate { get; set; }
        [FromQuery(Name = "toDate")]
        public DateTime? ToDate { get; set; }
    }
}
