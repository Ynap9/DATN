namespace sh.be.applications.Module.Construction.Dtos.DocumentType
{
    public class FindDocumentTypeByIdDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public int DefaultView { get; set; }
    }
}
