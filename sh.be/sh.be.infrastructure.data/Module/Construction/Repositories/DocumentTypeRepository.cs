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

        // Loại văn bản còn gắn với ít nhất một dự án chưa xoá (dòng gắn và dự án đều qua query filter xoá mềm)
        public Task<bool> IsUsedByProjectAsync(int id)
        {
            return _shDbContext.ProjectDocumentTypes.AnyAsync(pd => pd.DocumentTypeId == id
                && _shDbContext.Projects.Any(p => p.Id == pd.ProjectId));
        }

        // Người dùng là thành viên chưa gỡ ở ít nhất một cặp dự án – loại văn bản đang gắn (dòng gắn chưa xoá)
        public Task<bool> IsAssignedToUserAsync(int id, string userId)
        {
            return _shDbContext.ProjectDocumentTypes.AnyAsync(pd => pd.DocumentTypeId == id
                && _shDbContext.ProjectMembers.Any(m => m.ProjectId == pd.ProjectId && m.DocumentTypeId == pd.DocumentTypeId && m.UserId == userId));
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
