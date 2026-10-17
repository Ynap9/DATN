using sh.be.domain.Module.Construction.Entities;

namespace sh.be.domain.Module.Construction.Repositories
{
    public interface IDocumentTypeRepository
    {
        Task<DocumentType?> FindByIdAsync(int id);
        Task LockAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludedId = null);
        Task<int> CountActiveAsync(List<int> ids);
        void Add(DocumentType documentType);
        void Update(DocumentType documentType);
    }
}
