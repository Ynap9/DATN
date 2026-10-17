namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ImportOverwriteSubmissionDto : ImportSubmissionDto
    {
        public string Url { get; set; } = String.Empty;
        public string SheetName { get; set; } = String.Empty;
        public string ColumnImport { get; set; } = String.Empty;
        public int DocumentFieldId { get; set; }
        public bool OverwriteChecked { get; set; }
    }
}
