namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ViewImportSubmissionResultDto
    {
        public string Message { get; set; } = String.Empty;
        public int TotalDataFields { get; set; }
        public double DurationSeconds { get; set; }
    }
}
