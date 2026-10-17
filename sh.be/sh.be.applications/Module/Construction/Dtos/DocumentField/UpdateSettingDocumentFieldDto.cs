namespace sh.be.applications.Module.Construction.Dtos.DocumentField
{
    public class UpdateSettingDocumentFieldDto
    {
        public bool IsShow { get; set; }
        public bool IsChargeable { get; set; }
        public bool IsFieldOcr { get; set; }
        public string? FieldNameOcr { get; set; }
    }
}
