using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Construction.Entities;
using sh.be.domain.Module.Construction.Repositories;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Module.Construction.Repositories
{
    public class ProjectConfigRepository : IProjectConfigRepository
    {
        private readonly ShDbContext _shDbContext;

        public ProjectConfigRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public Task<ProjectConfig?> FindByIdAsync(int id)
        {
            return _shDbContext.ProjectConfigs.FirstOrDefaultAsync(p => p.Id == id);
        }

        public Task<ProjectConfig?> FindByNameAsync(string name)
        {
            return _shDbContext.ProjectConfigs.FirstOrDefaultAsync(p => p.Name == name);
        }

        public Task<List<ProjectConfig>> FindByNamesAsync(IReadOnlyCollection<string> names)
        {
            return _shDbContext.ProjectConfigs
                .Where(p => names.Contains(p.Name))
                .ToListAsync();
        }

        public Task<bool> ExistsByNameAsync(string name, int? excludedId = null)
        {
            return _shDbContext.ProjectConfigs.AnyAsync(p => p.Name == name && p.Id != excludedId);
        }

        public void Add(ProjectConfig projectConfig)
        {
            _shDbContext.ProjectConfigs.Add(projectConfig);
        }

        public void Update(ProjectConfig projectConfig)
        {
            _shDbContext.ProjectConfigs.Update(projectConfig);
        }
    }
}
