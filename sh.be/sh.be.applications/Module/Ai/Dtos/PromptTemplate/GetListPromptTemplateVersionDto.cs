namespace sh.be.applications.Module.Ai.Dtos.PromptTemplate
{
    public class GetListPromptTemplateVersionDto
    {
        public int Id { get; set; }
        public int Version { get; set; }
        public string Status { get; set; } = String.Empty;
        public string? Note { get; set; }
        public int? SourceVersion { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? PublishedByName { get; set; }
        public DateTime? PublishedDate { get; set; }
    }
}
