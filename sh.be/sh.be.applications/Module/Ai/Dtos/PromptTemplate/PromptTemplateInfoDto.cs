namespace sh.be.applications.Module.Ai.Dtos.PromptTemplate
{
    public class PromptTemplateInfoDto
    {
        public int Id { get; set; }
        public int Version { get; set; }
        public string Status { get; set; } = String.Empty;
        public string? Note { get; set; }
        public int? SourceVersion { get; set; }
        public string? ModifiedByName { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? PublishedByName { get; set; }
        public DateTime? PublishedDate { get; set; }
    }
}
