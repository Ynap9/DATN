using sh.be.domain.Module.Construction.Entities;

namespace sh.be.domain.Module.Construction.Repositories
{
    public interface IDocumentTypeRepository
    {
        Task<DocumentType?> FindByIdAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludedId = null);
        void Add(DocumentType documentType);
        void Update(DocumentType documentType);
    }
}
