using sh.be.applications.Module.Construction.Dtos.Project;

namespace sh.be.applications.Module.Construction.Interfaces.Services
{
    public interface IProjectService
    {
        Task<FindProjectByIdDto> FindById(int id);
        Task<FindProjectByIdDto> Create(CreateProjectDto dto);
        Task Update(int id, UpdateProjectDto dto);
        Task Delete(int id);
    }
}
