using sh.be.domain.Module.Construction.Entities;

namespace sh.be.domain.Module.Construction.Repositories
{
    public interface IDocumentFieldRepository
    {
        Task<DocumentField?> FindByIdAsync(int id);
        Task<List<DocumentField>> FindByDocumentTypeIdAndIdsAsync(int documentTypeId, IReadOnlyCollection<int> ids);
        Task<bool> ExistsByFieldAsync(int documentTypeId, string field, int? excludedId = null);
        Task<int> GetMaxOrderAsync(int documentTypeId);
        Task<List<DocumentField>> GetByDocumentTypeIdAsync(int documentTypeId);
        void Add(DocumentField documentField);
        void AddRange(IEnumerable<DocumentField> documentFields);
        void Update(DocumentField documentField);
    }
}
