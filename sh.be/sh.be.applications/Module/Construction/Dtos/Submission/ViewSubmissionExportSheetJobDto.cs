namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ViewSubmissionExportSheetJobDto
    {
        public string JobId { get; set; } = String.Empty;
        public int Status { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
