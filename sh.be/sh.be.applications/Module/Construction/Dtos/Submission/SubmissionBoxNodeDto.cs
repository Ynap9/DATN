namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class SubmissionBoxNodeDto
    {
        public string Box { get; set; } = String.Empty;
        public SubmissionTreeCountDto Count { get; set; } = new();
    }
}
