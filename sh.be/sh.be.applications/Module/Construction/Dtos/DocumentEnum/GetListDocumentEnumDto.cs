namespace sh.be.applications.Module.Construction.Dtos.DocumentEnum
{
    public class GetListDocumentEnumDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public List<string> Values { get; set; } = new();
        public List<DocumentEnumFieldDto> UsedByFields { get; set; } = new();
    }
}
