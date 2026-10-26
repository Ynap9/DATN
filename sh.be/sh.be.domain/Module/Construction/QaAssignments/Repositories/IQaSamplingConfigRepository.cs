using sh.be.domain.Module.Construction.QaAssignments.Entities;

namespace sh.be.domain.Module.Construction.QaAssignments.Repositories
{
    public interface IQaSamplingConfigRepository
    {
        Task<QaSamplingConfig?> FindByProjectAsync(int projectId);
        void Add(QaSamplingConfig config);
        void Update(QaSamplingConfig config);
    }
}
