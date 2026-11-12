using Microsoft.AspNetCore.Mvc;

namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ExportSubmissionDto : SubmissionFilterDto
    {
        [FromQuery(Name = "columns")]
        public List<string>? Columns { get; set; }
    }
}
