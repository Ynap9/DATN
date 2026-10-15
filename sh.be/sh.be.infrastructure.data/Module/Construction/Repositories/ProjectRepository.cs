using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Construction.Entities;
using sh.be.domain.Module.Construction.Repositories;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Module.Construction.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly ShDbContext _shDbContext;

        public ProjectRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public Task<Project?> FindByIdAsync(int id)
        {
            return _shDbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
        }

        public Task<bool> ExistsByNameAsync(string name, int? excludedId = null)
        {
            return _shDbContext.Projects.AnyAsync(p => p.Name == name && p.Id != excludedId);
        }

        public void Add(Project project)
        {
            _shDbContext.Projects.Add(project);
        }

        public void Update(Project project)
        {
            _shDbContext.Projects.Update(project);
        }
    }
}
