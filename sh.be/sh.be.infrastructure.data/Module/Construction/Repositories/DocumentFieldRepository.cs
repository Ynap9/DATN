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

        // Danh mục chưa xoá theo Id, chỉ khi thuộc đúng loại văn bản
        public Task<DocumentEnum?> FindEnumByIdAsync(int documentTypeId, int enumId)
        {
            return _shDbContext.DocumentEnums.FirstOrDefaultAsync(e => e.DocumentTypeId == documentTypeId && e.Id == enumId);
        }

        // Mọi danh mục chưa xoá của loại văn bản
        public Task<List<DocumentEnum>> GetEnumsByDocumentTypeIdAsync(int documentTypeId)
        {
            return _shDbContext.DocumentEnums
                .AsNoTracking()
                .Where(e => e.DocumentTypeId == documentTypeId)
                .ToListAsync();
        }

        // Tên danh mục đã có trong loại văn bản (bỏ qua danh mục đang sửa)
        public Task<bool> ExistsEnumByNameAsync(int documentTypeId, string name, int? excludedId = null)
        {
            return _shDbContext.DocumentEnums.AnyAsync(e => e.DocumentTypeId == documentTypeId && e.Name == name && e.Id != excludedId);
        }

        // Tên các trường chưa xoá đang trỏ tới danh mục
        public Task<List<string>> GetFieldNamesByEnumIdAsync(int enumId)
        {
            return _shDbContext.DocumentFields
                .AsNoTracking()
                .Where(d => d.EnumId == enumId)
                .OrderBy(d => d.OrderTableView)
                .ThenBy(d => d.Id)
                .Select(d => d.Name)
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

        // Thêm danh mục vào ngữ cảnh, chờ SaveChanges
        public void AddEnum(DocumentEnum documentEnum)
        {
            _shDbContext.DocumentEnums.Add(documentEnum);
        }

        // Đánh dấu danh mục đã sửa (gồm xoá mềm), chờ SaveChanges
        public void UpdateEnum(DocumentEnum documentEnum)
        {
            _shDbContext.DocumentEnums.Update(documentEnum);
        }
    }
}
