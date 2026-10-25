namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ViewSubmissionExportJobDto
    {
        public string JobId { get; set; } = String.Empty;
        public int Status { get; set; }
        public string FileName { get; set; } = String.Empty;
        public string? DownloadUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
