namespace sh.be.applications.Module.Ai.Dtos.PromptTemplate
{
    public class PromptFieldEditorDto
    {
        public int DocumentFieldId { get; set; }
        public string Name { get; set; } = String.Empty;
        public string? OutputKey { get; set; }
        public string Description { get; set; } = String.Empty;
        public string? Example { get; set; }
        public string NormalizeRule { get; set; } = String.Empty;
        public bool IsRequired { get; set; }
        public bool IsActive { get; set; }
        public bool InTemplate { get; set; }
    }
}
