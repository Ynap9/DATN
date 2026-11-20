namespace sh.be.applications.Module.Ai.Dtos.PromptTemplate
{
    public class SavePromptDraftDto
    {
        public string SystemPrompt { get; set; } = String.Empty;
        public string InstructionPrompt { get; set; } = String.Empty;
        public string? Note { get; set; }
        public List<SavePromptFieldDto> Fields { get; set; } = new();
    }
}
