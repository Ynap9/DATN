namespace sh.be.applications.Module.Construction.Dtos.DocumentField
{
    public class UpdateDocumentFieldDto
    {
        public string Name { get; set; } = String.Empty;
        public string Field { get; set; } = String.Empty;
        public string FieldType { get; set; } = "string";
        public string? FieldImport { get; set; }
        public bool IsShow { get; set; }
        public int Order { get; set; }
        public bool IsShowTableView { get; set; } = true;
        public bool IsShowListView { get; set; } = true;
        public int? Width { get; set; }
        public bool IsChargeable { get; set; }
        public string? FieldNameOcr { get; set; }
        public bool IsFieldOcr { get; set; }
    }
}
