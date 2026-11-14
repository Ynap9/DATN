using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Module.Construction.Dtos.Project;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Interfaces.Services
{
    public interface IProjectService
    {
        Task<List<GetListProjectDto>> GetList();
        Task<List<GetListProjectDropDownDto>> GetProjectDropDown();
        Task<BaseResponsePagingDto<GetListProjectPairDto>> FindPaging(FindPagingProjectDto dto);
        Task<FindProjectByIdDto> FindById(int id);
        Task<ViewProjectAcceptanceDto> FindAcceptance(int id);
        Task<ViewProjectAcceptanceDto> StartAcceptance(int id, TransitionAcceptanceProjectDto dto);
        Task<ViewProjectAcceptanceDto> FinishAcceptance(int id, TransitionAcceptanceProjectDto dto);
        Task<ViewProjectAcceptanceDto> ReopenAcceptance(int id, ReopenAcceptanceProjectDto dto);
        Task<ExportFileDto> ExportAcceptanceReport(int id);
        Task<FindProjectByIdDto> Create(CreateProjectDto dto);
        Task Update(int id, UpdateProjectDto dto);
        Task Delete(int id);
        Task DeleteCascade(int id);
    }
}
