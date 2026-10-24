using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Construction.DocumentFields.Entities;
using sh.be.domain.Module.Construction.DocumentFields.Repositories;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Module.Construction.Repositories
{
    public class DocumentFieldRepository : IDocumentFieldRepository
    {
        private readonly ShDbContext _shDbContext;

        public DocumentFieldRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public Task<DocumentField?> FindByIdAsync(int id)
        {
            return _shDbContext.DocumentFields.FirstOrDefaultAsync(d => d.Id == id);
        }

        public Task<int?> FindDocumentTypeIdAsync(int id)
        {
            return _shDbContext.DocumentFields
                .Where(d => d.Id == id)
                .Select(d => (int?)d.DocumentTypeId)
                .FirstOrDefaultAsync();
        }

        public Task<List<DocumentField>> FindByDocumentTypeIdAndIdsAsync(int documentTypeId, IReadOnlyCollection<int> ids)
        {
            return _shDbContext.DocumentFields
                .Where(d => d.DocumentTypeId == documentTypeId && ids.Contains(d.Id))
                .ToListAsync();
        }

        public Task<bool> ExistsByFieldAsync(int documentTypeId, string field, int? excludedId = null)
        {
            return _shDbContext.DocumentFields.AnyAsync(d => d.DocumentTypeId == documentTypeId && d.Field == field && d.Id != excludedId);
        }

        public async Task<int> GetMaxOrderAsync(int documentTypeId)
        {
            return await _shDbContext.DocumentFields
                .IgnoreQueryFilters()
                .Where(d => d.DocumentTypeId == documentTypeId)
                .MaxAsync(d => (int?)(d.OrderTableView > d.OrderListView ? d.OrderTableView : d.OrderListView)) ?? 0;
        }

        public Task<List<DocumentField>> GetByDocumentTypeIdAsync(int documentTypeId)
        {
            return _shDbContext.DocumentFields
                .Where(d => d.DocumentTypeId == documentTypeId)
                .OrderBy(d => d.OrderTableView)
                .ThenBy(d => d.Id)
                .ToListAsync();
        }

        public Task<List<DocumentField>> GetByDocumentTypeIdsAsync(IReadOnlyCollection<int> documentTypeIds)
        {
            return _shDbContext.DocumentFields
                .AsNoTracking()
                .Where(d => documentTypeIds.Contains(d.DocumentTypeId))
                .ToListAsync();
        }

        public void Add(DocumentField documentField)
        {
            _shDbContext.DocumentFields.Add(documentField);
        }

        public void AddRange(IEnumerable<DocumentField> documentFields)
        {
            _shDbContext.DocumentFields.AddRange(documentFields);
        }

        public void Update(DocumentField documentField)
        {
            _shDbContext.DocumentFields.Update(documentField);
        }
    }
}
