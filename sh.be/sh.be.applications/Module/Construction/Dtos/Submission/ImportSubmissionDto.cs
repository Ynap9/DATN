namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ImportSubmissionDto
    {
        public int ProjectId { get; set; }
        public int DocumentTypeId { get; set; }
        public string FileColumnImport { get; set; } = String.Empty;
        public string LinkColumnImport { get; set; } = String.Empty;
        public string ProfileRange { get; set; } = String.Empty;
        public string Box { get; set; } = String.Empty;
    }
}
