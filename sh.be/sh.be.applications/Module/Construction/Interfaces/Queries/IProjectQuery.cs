using sh.be.applications.Module.Construction.Dtos.Project;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Interfaces.Queries
{
    public interface IProjectQuery
    {
        Task<List<GetListProjectDto>> GetList();
        Task<List<GetListProjectDropDownDto>> GetProjectDropDown();
        Task<BaseResponsePagingDto<GetListProjectDto>> FindPaging(FindPagingProjectDto dto);
        Task<FindProjectByIdDto?> FindById(int id);
    }
}
