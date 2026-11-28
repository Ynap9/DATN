namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ViewVerifyImportBlankSubmissionDto
    {
        public int TotalBlankCount { get; set; }
        public int TotalFieldCount { get; set; }
        public List<OutOfEnumFieldDto> OutOfEnum { get; set; } = new();
    }
}
