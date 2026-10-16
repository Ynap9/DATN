using sh.be.applications.Module.Construction.Dtos.Project;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Interfaces.Services
{
    public interface IProjectService
    {
        Task<List<GetListProjectDropDownDto>> GetProjectDropDown();
        Task<BaseResponsePagingDto<GetListProjectDto>> FindPaging(FindPagingProjectDto dto);
        Task<FindProjectByIdDto> FindById(int id);
        Task<FindProjectByIdDto> Create(CreateProjectDto dto);
        Task Update(int id, UpdateProjectDto dto);
        Task Delete(int id);
    }
}
