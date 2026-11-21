using Microsoft.AspNetCore.Mvc;

namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class FindProgressStatsDto
    {
        [FromQuery(Name = "projectId")]
        public int ProjectId { get; set; }

        [FromQuery(Name = "documentTypeId")]
        public int DocumentTypeId { get; set; }

        [FromQuery(Name = "fromDate")]
        public DateTime? FromDate { get; set; }

        [FromQuery(Name = "toDate")]
        public DateTime? ToDate { get; set; }
    }
}
