using sh.be.domain.Module.Construction.Submissions.Services;

namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class SubmissionProfileSummarySourceDto
    {
        public List<string> FieldNames { get; set; } = new();
        public List<ProfileSummarySubmission> Submissions { get; set; } = new();
    }
}
