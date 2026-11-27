namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class GetImportSheetHeadersDto
    {
        public int ProjectId { get; set; }
        public int DocumentTypeId { get; set; }
        public string Url { get; set; } = String.Empty;
    }
}
