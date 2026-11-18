using sh.be.domain.Module.Construction.DocumentTypes.Entities;

namespace sh.be.domain.Module.Construction.DocumentTypes.Repositories
{
    public interface IDocumentTypeRepository
    {
        Task<DocumentType?> FindByIdAsync(int id);
        Task LockAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludedId = null);
        Task<int> CountActiveAsync(List<int> ids);
        Task<bool> IsUsedByProjectAsync(int id);
        Task<bool> IsAssignedToUserAsync(int id, string userId);
        void Add(DocumentType documentType);
        void Update(DocumentType documentType);
    }
}
