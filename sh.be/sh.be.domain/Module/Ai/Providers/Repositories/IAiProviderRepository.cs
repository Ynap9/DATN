using sh.be.domain.Module.Ai.Providers.Entities;

namespace sh.be.domain.Module.Ai.Providers.Repositories
{
    public interface IAiProviderRepository
    {
        Task<AiProvider?> FindByIdAsync(int id);
        Task<bool> ExistsByCodeAsync(string code);
        Task<bool> HasModelsAsync(int id);
        Task<bool> IsUsedByProjectAsync(int id);
        void Add(AiProvider aiProvider);
        void Update(AiProvider aiProvider);
    }
}
