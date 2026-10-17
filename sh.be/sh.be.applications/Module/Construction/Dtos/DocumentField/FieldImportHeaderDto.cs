namespace sh.be.applications.Module.Construction.Dtos.DocumentField
{
    public class FieldImportHeaderDto
    {
        public string? FieldImport { get; set; }
        public List<string> Headers { get; set; } = new();
    }
}
