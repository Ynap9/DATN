namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ViewSubmissionTreeDto
    {
        public List<SubmissionProfileRangeNodeDto> ProfileRanges { get; set; } = new();
        public SubmissionTreeCountDto Total { get; set; } = new();
    }
}
