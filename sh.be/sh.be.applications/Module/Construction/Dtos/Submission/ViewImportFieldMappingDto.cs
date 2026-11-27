namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ViewImportFieldMappingDto
    {
        public int DocumentFieldId { get; set; }
        public string Name { get; set; } = String.Empty;
        public string FieldImport { get; set; } = String.Empty;
        public string MatchedHeader { get; set; } = String.Empty;
        public int? MatchedColumnIndex { get; set; }
    }
}
