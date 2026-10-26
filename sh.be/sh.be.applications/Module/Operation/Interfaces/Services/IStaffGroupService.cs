using sh.be.applications.Module.Operation.Dtos.StaffGroup;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Operation.Interfaces.Services
{
    public interface IStaffGroupService
    {
        Task<BaseResponsePagingDto<ViewStaffGroupDto>> FindPaging(FindPagingStaffGroupDto dto);
        Task<FindStaffGroupByIdDto> FindById(int id);
        Task<FindStaffGroupByIdDto> Create(CreateStaffGroupDto dto);
        Task Update(int id, UpdateStaffGroupDto dto);
        Task Delete(int id);
        Task<FindStaffGroupByIdDto> AddMembers(int id, ChangeStaffGroupMembersDto dto);
        Task<FindStaffGroupByIdDto> RemoveMembers(int id, ChangeStaffGroupMembersDto dto);
        Task<BaseResponsePagingDto<StaffGroupCandidateDto>> FindCandidates(FindStaffGroupCandidateDto dto);
    }
}
