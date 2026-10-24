using Microsoft.AspNetCore.Mvc;

namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class FindSubmissionSummaryDto
    {
        [FromQuery(Name = "projectId")]
        public int ProjectId { get; set; }

        [FromQuery(Name = "documentTypeId")]
        public int DocumentTypeId { get; set; }
    }
}
