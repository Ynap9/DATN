using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Construction.DocumentTypes.Entities;
using sh.be.domain.Module.Construction.DocumentTypes.Repositories;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Module.Construction.Repositories
{
    public class DocumentTypeRepository : IDocumentTypeRepository
    {
        private readonly ShDbContext _shDbContext;

        public DocumentTypeRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public Task<DocumentType?> FindByIdAsync(int id)
        {
            return _shDbContext.DocumentTypes.FirstOrDefaultAsync(d => d.Id == id);
        }

        public Task LockAsync(int id)
        {
            return _shDbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT Id FROM [construction].[DocumentType] WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}");
        }

        public Task<bool> ExistsByNameAsync(string name, int? excludedId = null)
        {
            return _shDbContext.DocumentTypes.AnyAsync(d => d.Name == name && d.Id != excludedId);
        }

        public Task<int> CountActiveAsync(List<int> ids)
        {
            return _shDbContext.DocumentTypes.CountAsync(d => ids.Contains(d.Id));
        }

        public void Add(DocumentType documentType)
        {
            _shDbContext.DocumentTypes.Add(documentType);
        }

        public void Update(DocumentType documentType)
        {
            _shDbContext.DocumentTypes.Update(documentType);
        }
    }
}
