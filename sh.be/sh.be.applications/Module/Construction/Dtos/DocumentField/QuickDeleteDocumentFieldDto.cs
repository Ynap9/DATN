namespace sh.be.applications.Module.Construction.Dtos.DocumentField
{
    public class QuickDeleteDocumentFieldDto
    {
        public int DocumentTypeId { get; set; }
        public List<int> Ids { get; set; } = new();
    }
}
