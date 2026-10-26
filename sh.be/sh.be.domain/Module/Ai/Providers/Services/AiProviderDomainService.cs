using sh.be.domain.Module.Ai.Providers.Entities;
using sh.be.domain.Module.Ai.Providers.Repositories;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Ai.Providers.Services
{
    public class AiProviderDomainService
    {
        private readonly IAiProviderRepository _aiProviderRepository;

        public AiProviderDomainService(IAiProviderRepository aiProviderRepository)
        {
            _aiProviderRepository = aiProviderRepository;
        }

        public async Task EnsureUniqueCodeAsync(string code)
        {
            if (await _aiProviderRepository.ExistsByCodeAsync(code))
            {
                throw new UserFriendlyException(ErrorCodes.AiErrorProviderCodeExists);
            }
        }

        public async Task EnsureProtocolChangeAllowedAsync(AiProvider aiProvider, string newProtocol)
        {
            if (aiProvider.Protocol != newProtocol && await _aiProviderRepository.HasModelsAsync(aiProvider.Id))
            {
                throw new UserFriendlyException(ErrorCodes.AiErrorProviderProtocolLocked);
            }
        }

        public async Task EnsureCanDeactivateAsync(AiProvider aiProvider, bool newIsActive)
        {
            if (aiProvider.IsActive && !newIsActive && await _aiProviderRepository.IsUsedByProjectAsync(aiProvider.Id))
            {
                throw new UserFriendlyException(ErrorCodes.AiErrorProviderInUse);
            }
        }

        public async Task EnsureDeletableAsync(AiProvider aiProvider)
        {
            if (await _aiProviderRepository.HasModelsAsync(aiProvider.Id))
            {
                throw new UserFriendlyException(ErrorCodes.AiErrorProviderHasModels);
            }
        }
    }
}
