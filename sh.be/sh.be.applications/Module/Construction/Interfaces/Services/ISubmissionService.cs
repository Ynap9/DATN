using sh.be.applications.Module.Construction.Dtos.Submission;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Interfaces.Services
{
    public interface ISubmissionService
    {
        Task<BaseResponsePagingDto<GetListSubmissionDto>> FindPaging(FindPagingSubmissionDto dto);
        Task<ViewSubmissionDetailDto> FindById(int id, FindSubmissionDetailDto dto);
        Task<ViewSubmissionDetailDto> FindNeighbor(int id, FindSubmissionNeighborDto dto);
        Task<ViewCountSubmissionDto> Count(SubmissionFilterDto dto);
    }
}
