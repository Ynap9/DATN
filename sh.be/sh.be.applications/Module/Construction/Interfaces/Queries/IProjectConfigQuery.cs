using sh.be.applications.Module.Construction.Dtos.ProjectConfig;

namespace sh.be.applications.Module.Construction.Interfaces.Queries
{
    public interface IProjectConfigQuery
    {
        Task<List<GetListProjectConfigDto>> GetList();
        Task<FindProjectConfigByIdDto?> FindById(int id);
    }
}
