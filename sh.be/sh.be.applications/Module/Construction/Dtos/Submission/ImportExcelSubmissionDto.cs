namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ImportExcelSubmissionDto : ImportSubmissionDto
    {
        public string FileKey { get; set; } = String.Empty;
        public string SheetName { get; set; } = String.Empty;
    }
}
