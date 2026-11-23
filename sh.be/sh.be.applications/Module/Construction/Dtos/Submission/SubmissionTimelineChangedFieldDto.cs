namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class SubmissionTimelineChangedFieldDto
    {
        public int DocumentFieldId { get; set; }
        public string Name { get; set; } = String.Empty;
        public string OldData { get; set; } = String.Empty;
        public string NewData { get; set; } = String.Empty;
    }
}
