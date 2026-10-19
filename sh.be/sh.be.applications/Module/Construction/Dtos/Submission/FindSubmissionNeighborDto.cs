using Microsoft.AspNetCore.Mvc;

namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class FindSubmissionNeighborDto : SubmissionFilterDto
    {
        [FromQuery(Name = "direction")]
        public string Direction { get; set; } = "next";
    }
}
