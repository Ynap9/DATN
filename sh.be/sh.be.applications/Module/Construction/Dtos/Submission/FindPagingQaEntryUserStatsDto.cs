using Microsoft.AspNetCore.Mvc;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class FindPagingQaEntryUserStatsDto : BaseRequestPagingDto
    {
        [FromQuery(Name = "projectId")]
        public int ProjectId { get; set; }

        [FromQuery(Name = "documentTypeId")]
        public int DocumentTypeId { get; set; }

        [FromQuery(Name = "fromDate")]
        public DateTime? FromDate { get; set; }

        [FromQuery(Name = "toDate")]
        public DateTime? ToDate { get; set; }

        [FromQuery(Name = "orderBy")]
        public string? OrderBy { get; set; }
    }
}
