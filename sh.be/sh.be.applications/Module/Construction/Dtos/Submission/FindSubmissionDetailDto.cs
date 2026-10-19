using Microsoft.AspNetCore.Mvc;

namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class FindSubmissionDetailDto : SubmissionFilterDto
    {
        [FromQuery(Name = "includePosition")]
        public bool IncludePosition { get; set; }
    }
}
