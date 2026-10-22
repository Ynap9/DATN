namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class SubmissionExportTableDto
    {
        public string ProjectName { get; set; } = String.Empty;
        public string DocumentTypeName { get; set; } = String.Empty;
        public List<string> Headers { get; set; } = new();
        public List<List<string>> Rows { get; set; } = new();
    }
}
