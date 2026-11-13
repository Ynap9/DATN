using sh.be.applications.Module.Construction.Dtos.Project;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Interfaces.Queries
{
    public interface IProjectQuery
    {
        Task<List<GetListProjectDto>> GetList(string userId, bool isAdmin);
        Task<List<GetListProjectDropDownDto>> GetProjectDropDown(string userId, bool isAdmin);
        Task<BaseResponsePagingDto<GetListProjectPairDto>> FindPaging(FindPagingProjectDto dto, string userId, bool isAdmin);
        Task<FindProjectByIdDto?> FindById(int id);
        Task<List<GetListProjectMyProgressDto>> FindMyProgresses(int projectId, string userId);
        Task<ViewProjectAcceptanceDto> FindAcceptanceSummary(int projectId);
        Task<ViewProjectAcceptanceTransitionDto?> FindLastAcceptanceTransition(int projectId);
    }
}
