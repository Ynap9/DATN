namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class SubmissionProfileRangeNodeDto
    {
        public string ProfileRange { get; set; } = String.Empty;
        public SubmissionTreeCountDto Count { get; set; } = new();
        public List<SubmissionBoxNodeDto> Boxes { get; set; } = new();
    }
}
