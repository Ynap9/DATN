namespace sh.be.applications.Module.Construction.Dtos.DocumentType
{
    public class GetListDocumentTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public int DefaultView { get; set; }
        public int FieldCount { get; set; }
        public int ProjectCount { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
