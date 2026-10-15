using sh.be.domain.Module.Construction.Entities;

namespace sh.be.domain.Module.Construction.Repositories
{
    public interface IProjectRepository
    {
        Task<Project?> FindByIdAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludedId = null);
        void Add(Project project);
        void Update(Project project);
    }
}
