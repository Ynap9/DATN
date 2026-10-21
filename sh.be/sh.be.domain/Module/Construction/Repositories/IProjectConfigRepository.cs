using sh.be.domain.Module.Construction.Entities;

namespace sh.be.domain.Module.Construction.Repositories
{
    public interface IProjectConfigRepository
    {
        Task<ProjectConfig?> FindByIdAsync(int id);
        Task<ProjectConfig?> FindByNameAsync(string name);
        Task<List<ProjectConfig>> FindByNamesAsync(IReadOnlyCollection<string> names);
        Task<bool> ExistsByNameAsync(string name, int? excludedId = null);
        void Add(ProjectConfig projectConfig);
        void Update(ProjectConfig projectConfig);
    }
}
