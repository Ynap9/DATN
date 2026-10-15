using sh.be.applications.Module.Construction.Dtos.Project;

namespace sh.be.applications.Module.Construction.Interfaces.Queries
{
    public interface IProjectQuery
    {
        Task<FindProjectByIdDto?> FindById(int id);
    }
}
