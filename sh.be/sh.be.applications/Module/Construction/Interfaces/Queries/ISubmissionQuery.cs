using sh.be.applications.Module.Construction.Dtos.Submission;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Interfaces.Queries
{
    public interface ISubmissionQuery
    {
        Task<BaseResponsePagingDto<GetListSubmissionDto>> FindPaging(FindPagingSubmissionDto dto);
        Task<ViewSubmissionDetailDto?> FindById(int id, ISubmissionFilter filter, bool includePosition);
        Task<int?> FindNextId(int id, ISubmissionFilter filter);
        Task<int?> FindPreviousId(int id, ISubmissionFilter filter);
        Task<ViewCountSubmissionDto> Count(ISubmissionFilter filter);
        Task<BaseResponsePagingDto<GetListSubmissionLogDto>> FindLogPaging(int submissionId, FindPagingSubmissionLogDto dto);
    }
}
