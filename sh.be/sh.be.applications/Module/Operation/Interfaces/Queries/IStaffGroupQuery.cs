using sh.be.applications.Module.Operation.Dtos.StaffGroup;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Operation.Interfaces.Queries
{
    public interface IStaffGroupQuery
    {
        Task<BaseResponsePagingDto<ViewStaffGroupDto>> FindPaging(FindPagingStaffGroupDto dto);
        Task<FindStaffGroupByIdDto?> FindById(int id);
        Task<BaseResponsePagingDto<StaffGroupCandidateDto>> FindCandidates(FindStaffGroupCandidateDto dto);
    }
}
