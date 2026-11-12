namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ImportExcelOverwriteSubmissionDto : ImportExcelSubmissionDto, IImportOverwriteOptions
    {
        public string ColumnImport { get; set; } = String.Empty;
        public int DocumentFieldId { get; set; }
        public bool OverwriteChecked { get; set; }
    }
}
