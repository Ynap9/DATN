using sh.be.domain.Module.Ai.Prompts.Entities;

namespace sh.be.domain.Module.Ai.Prompts.Repositories
{
    public interface IPromptFieldConfigRepository
    {
        Task<List<PromptFieldConfig>> GetByTemplateIdAsync(int promptTemplateId);
        void AddRange(List<PromptFieldConfig> promptFieldConfigs);
    }
}
