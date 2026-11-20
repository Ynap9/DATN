using Microsoft.AspNetCore.Mvc;

namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class FindLatestWorkingSubmissionDto
    {
        [FromQuery(Name = "projectId")]
        public int ProjectId { get; set; }

        [FromQuery(Name = "documentTypeId")]
        public int DocumentTypeId { get; set; }

        [FromQuery(Name = "type")]
        public string? Type { get; set; }
    }
}
