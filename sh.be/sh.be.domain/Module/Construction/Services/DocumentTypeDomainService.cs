using sh.be.domain.Module.Construction.Repositories;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Construction.Services
{
    public class DocumentTypeDomainService
    {
        private readonly IDocumentTypeRepository _documentTypeRepository;

        public DocumentTypeDomainService(IDocumentTypeRepository documentTypeRepository)
        {
            _documentTypeRepository = documentTypeRepository;
        }

        public async Task EnsureUniqueNameAsync(string name, int? excludedId = null)
        {
            if (await _documentTypeRepository.ExistsByNameAsync(name, excludedId))
            {
                throw new UserFriendlyException(ErrorCodes.Found, "Tên loại văn bản đã tồn tại");
            }
        }
    }
}
