using sh.be.domain.Module.Ai.Prompts.Entities;

namespace sh.be.domain.Module.Ai.Prompts.Repositories
{
    public interface IPromptTemplateRepository
    {
        Task<PromptTemplate?> FindByIdAsync(int id);
        Task<PromptTemplate?> FindDraftAsync(int documentTypeId);
        Task<PromptTemplate?> FindPublishedAsync(int documentTypeId);
        Task<int> GetNextVersionAsync(int documentTypeId);
        Task LockDocumentTypeAsync(int documentTypeId);
        void Add(PromptTemplate promptTemplate);
    }
}
