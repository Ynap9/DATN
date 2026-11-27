namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ImportSheetSubmissionDto : ImportSubmissionDto
    {
        public string Url { get; set; } = String.Empty;
        public string SheetName { get; set; } = String.Empty;
    }
}
