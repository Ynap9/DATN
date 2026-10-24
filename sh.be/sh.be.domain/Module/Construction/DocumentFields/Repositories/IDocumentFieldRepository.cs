using sh.be.domain.Module.Construction.DocumentFields.Entities;

namespace sh.be.domain.Module.Construction.DocumentFields.Repositories
{
    public interface IDocumentFieldRepository
    {
        Task<DocumentField?> FindByIdAsync(int id);
        Task<int?> FindDocumentTypeIdAsync(int id);
        Task<List<DocumentField>> FindByDocumentTypeIdAndIdsAsync(int documentTypeId, IReadOnlyCollection<int> ids);
        Task<bool> ExistsByFieldAsync(int documentTypeId, string field, int? excludedId = null);
        Task<int> GetMaxOrderAsync(int documentTypeId);
        Task<List<DocumentField>> GetByDocumentTypeIdAsync(int documentTypeId);
        Task<List<DocumentField>> GetByDocumentTypeIdsAsync(IReadOnlyCollection<int> documentTypeIds);
        void Add(DocumentField documentField);
        void AddRange(IEnumerable<DocumentField> documentFields);
        void Update(DocumentField documentField);
    }
}
