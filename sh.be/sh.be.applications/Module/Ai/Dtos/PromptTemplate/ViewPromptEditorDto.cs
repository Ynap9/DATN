namespace sh.be.applications.Module.Ai.Dtos.PromptTemplate
{
    public class ViewPromptEditorDto
    {
        public int DocumentTypeId { get; set; }
        public string DocumentTypeName { get; set; } = String.Empty;
        public PromptTemplateInfoDto? Published { get; set; }
        public PromptTemplateInfoDto? Draft { get; set; }
        public int? EditingId { get; set; }
        public string SystemPrompt { get; set; } = String.Empty;
        public string InstructionPrompt { get; set; } = String.Empty;
        public List<PromptFieldEditorDto> Fields { get; set; } = new();
        public List<string> MissingFields { get; set; } = new();
        public List<string> InvalidFields { get; set; } = new();
    }
}
