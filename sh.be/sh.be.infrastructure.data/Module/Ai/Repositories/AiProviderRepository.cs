using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Ai.Providers.Entities;
using sh.be.domain.Module.Ai.Providers.Repositories;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Module.Ai.Repositories
{
    public class AiProviderRepository : IAiProviderRepository
    {
        private readonly ShDbContext _shDbContext;

        public AiProviderRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public Task<AiProvider?> FindByIdAsync(int id)
        {
            return _shDbContext.AiProviders.FirstOrDefaultAsync(p => p.Id == id);
        }

        public Task<bool> ExistsByCodeAsync(string code)
        {
            return _shDbContext.AiProviders.AnyAsync(p => p.Code == code);
        }

        public Task<bool> HasModelsAsync(int id)
        {
            return _shDbContext.AiModels.AnyAsync(m => m.ProviderId == id);
        }

        public Task<bool> IsUsedByProjectAsync(int id)
        {
            return _shDbContext.ProjectAiConfigs.AnyAsync(c => _shDbContext.AiModels
                .Any(m => m.ProviderId == id && (m.Id == c.PrimaryModelId || m.Id == c.FallbackModelId)));
        }

        public void Add(AiProvider aiProvider)
        {
            _shDbContext.AiProviders.Add(aiProvider);
        }

        public void Update(AiProvider aiProvider)
        {
            _shDbContext.AiProviders.Update(aiProvider);
        }
    }
}
