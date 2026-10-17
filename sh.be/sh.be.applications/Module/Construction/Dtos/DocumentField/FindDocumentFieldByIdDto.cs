namespace sh.be.applications.Module.Construction.Dtos.DocumentField
{
    public class FindDocumentFieldByIdDto
    {
        public int Id { get; set; }
        public int DocumentTypeId { get; set; }
        public string Name { get; set; } = String.Empty;
        public string Field { get; set; } = String.Empty;
        public string FieldType { get; set; } = String.Empty;
        public string? FieldImport { get; set; }
        public bool IsShow { get; set; }
        public int OrderTableView { get; set; }
        public int OrderListView { get; set; }
        public bool IsShowTableView { get; set; }
        public bool IsShowListView { get; set; }
        public int? Width { get; set; }
        public bool IsChargeable { get; set; }
        public string? FieldNameOcr { get; set; }
        public bool IsFieldOcr { get; set; }
    }
}
