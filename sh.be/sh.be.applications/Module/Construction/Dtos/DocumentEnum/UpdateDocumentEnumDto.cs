namespace sh.be.applications.Module.Construction.Dtos.DocumentEnum
{
    public class UpdateDocumentEnumDto
    {
        public string Name { get; set; } = String.Empty;
        public List<string> Values { get; set; } = new();
    }
}
