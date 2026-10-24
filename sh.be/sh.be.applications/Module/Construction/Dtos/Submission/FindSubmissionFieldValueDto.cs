using Microsoft.AspNetCore.Mvc;

namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class FindSubmissionFieldValueDto : SubmissionFilterDto
    {
        [FromQuery(Name = "documentFieldId")]
        public int DocumentFieldId { get; set; }
    }
}
