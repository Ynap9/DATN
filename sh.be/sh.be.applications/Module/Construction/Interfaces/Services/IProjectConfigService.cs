using sh.be.applications.Module.Construction.Dtos.ProjectConfig;

namespace sh.be.applications.Module.Construction.Interfaces.Services
{
    public interface IProjectConfigService
    {
        Task<List<GetListProjectConfigDto>> GetList();
        Task<FindProjectConfigByIdDto> FindById(int id);
        Task Create(CreateProjectConfigDto dto);
        Task Update(int id, UpdateProjectConfigDto dto);
        Task Delete(int id);
        Task<string> GetGoogleAuthorizationUrl();
        Task CompleteGoogleAuthorization(GoogleAuthorizationCallbackDto dto);
    }
}
