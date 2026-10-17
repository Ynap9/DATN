using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.DigitalSignature.Entities;
using sh.be.domain.Module.DigitalSignature.Repositories;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Module.DigitalSignature.Repositories
{
    public class TemplateRepository : ITemplateRepository
    {
        private readonly ShDbContext _shDbContext;

        public TemplateRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public Task<Template?> FindByIdAsync(int id)
        {
            return _shDbContext.Templates.FirstOrDefaultAsync(t => t.Id == id);
        }

        public void Add(Template template)
        {
            _shDbContext.Templates.Add(template);
        }

        public void Update(Template template)
        {
            _shDbContext.Templates.Update(template);
        }

        public Task<List<TemplatePosition>> GetPositionsAsync(int templateId)
        {
            return _shDbContext.TemplatePositions
                .Where(tp => tp.TemplateId == templateId)
                .ToListAsync();
        }

        public async Task ReplacePositionsAsync(int templateId, List<TemplatePosition> positions)
        {
            await _shDbContext.TemplatePositions
                .IgnoreQueryFilters()
                .Where(tp => tp.TemplateId == templateId)
                .ExecuteDeleteAsync();

            _shDbContext.TemplatePositions.AddRange(positions);
        }
    }
}
