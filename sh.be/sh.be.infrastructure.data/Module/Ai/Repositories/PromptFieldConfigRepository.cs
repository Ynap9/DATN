using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Ai.Prompts.Entities;
using sh.be.domain.Module.Ai.Prompts.Repositories;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Module.Ai.Repositories
{
    public class PromptFieldConfigRepository : IPromptFieldConfigRepository
    {
        private readonly ShDbContext _shDbContext;

        public PromptFieldConfigRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        // Cấu hình trường chưa xoá của một bản prompt, theo thứ tự trong prompt
        public Task<List<PromptFieldConfig>> GetByTemplateIdAsync(int promptTemplateId)
        {
            return _shDbContext.PromptFieldConfigs
                .Where(c => c.PromptTemplateId == promptTemplateId)
                .OrderBy(c => c.Order)
                .ThenBy(c => c.Id)
                .ToListAsync();
        }

        // Thêm cấu hình trường mới, lưu khi SaveChanges
        public void AddRange(List<PromptFieldConfig> promptFieldConfigs)
        {
            _shDbContext.PromptFieldConfigs.AddRange(promptFieldConfigs);
        }
    }
}
