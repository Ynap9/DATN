using sh.be.shared.Constants.Ai;

namespace sh.be.applications.Module.Ai.Dtos.PromptTemplate
{
    public class SavePromptFieldDto
    {
        public int DocumentFieldId { get; set; }
        public string Description { get; set; } = String.Empty;
        public string? Example { get; set; }
        public string NormalizeRule { get; set; } = PromptTemplateConst.NORMALIZE_NONE;
        public bool IsRequired { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
