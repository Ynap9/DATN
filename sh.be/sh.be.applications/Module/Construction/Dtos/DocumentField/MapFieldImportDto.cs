namespace sh.be.applications.Module.Construction.Dtos.DocumentField
{
    public class MapFieldImportDto
    {
        public int DocumentTypeId { get; set; }
        public string Url { get; set; } = String.Empty;
        public string SheetName { get; set; } = String.Empty;
        public int HeaderStartIndex { get; set; } = 1;
    }
}
