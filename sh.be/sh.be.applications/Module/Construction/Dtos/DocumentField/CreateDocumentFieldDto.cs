namespace sh.be.applications.Module.Construction.Dtos.DocumentField
{
    public class CreateDocumentFieldDto
    {
        public int DocumentTypeId { get; set; }
        public string Name { get; set; } = String.Empty;
        public string Field { get; set; } = String.Empty;
        public string? FieldImport { get; set; }
        public bool IsShow { get; set; }
        public bool IsChargeable { get; set; }
        public string? FieldNameOcr { get; set; }
        public bool IsFieldOcr { get; set; }
    }
}
