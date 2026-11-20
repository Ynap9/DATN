using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Ai.Prompts.Entities;
using sh.be.domain.Module.Ai.Prompts.Repositories;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Ai;

namespace sh.be.infrastructure.data.Module.Ai.Repositories
{
    public class PromptTemplateRepository : IPromptTemplateRepository
    {
        private readonly ShDbContext _shDbContext;

        public PromptTemplateRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        // Bản prompt chưa xoá theo id
        public Task<PromptTemplate?> FindByIdAsync(int id)
        {
            return _shDbContext.PromptTemplates.FirstOrDefaultAsync(t => t.Id == id);
        }

        // Bản nháp của metadata (tối đa một bản)
        public Task<PromptTemplate?> FindDraftAsync(int documentTypeId)
        {
            return _shDbContext.PromptTemplates
                .FirstOrDefaultAsync(t => t.DocumentTypeId == documentTypeId && t.Status == PromptTemplateConst.STATUS_DRAFT);
        }

        // Bản đang dùng của metadata
        public Task<PromptTemplate?> FindPublishedAsync(int documentTypeId)
        {
            return _shDbContext.PromptTemplates
                .FirstOrDefaultAsync(t => t.DocumentTypeId == documentTypeId && t.Status == PromptTemplateConst.STATUS_PUBLISHED);
        }

        // Số phiên bản kế tiếp, tính cả bản đã xoá để không dùng lại số cũ
        public async Task<int> GetNextVersionAsync(int documentTypeId)
        {
            var maxVersion = await _shDbContext.PromptTemplates
                .IgnoreQueryFilters()
                .Where(t => t.DocumentTypeId == documentTypeId)
                .MaxAsync(t => (int?)t.Version);
            return (maxVersion ?? 0) + 1;
        }

        // Khoá dòng metadata tới hết transaction để tuần tự hoá việc ghi prompt của metadata đó
        public Task LockDocumentTypeAsync(int documentTypeId)
        {
            return _shDbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT Id FROM [construction].[DocumentType] WITH (UPDLOCK, HOLDLOCK) WHERE Id = {documentTypeId}");
        }

        // Thêm bản prompt mới, lưu khi SaveChanges
        public void Add(PromptTemplate promptTemplate)
        {
            _shDbContext.PromptTemplates.Add(promptTemplate);
        }
    }
}
