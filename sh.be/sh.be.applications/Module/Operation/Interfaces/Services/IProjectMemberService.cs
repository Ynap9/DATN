using sh.be.applications.Module.Operation.Dtos.ProjectMember;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Operation.Interfaces.Services
{
    public interface IProjectMemberService
    {
        Task<BaseResponsePagingDto<ViewProjectMemberDto>> FindPaging(int projectId, FindPagingProjectMemberDto dto);
        Task<BaseResponsePagingDto<ProjectMemberCandidateDto>> FindCandidates(int projectId, FindProjectMemberCandidateDto dto);
        Task<AddProjectMemberResultDto> AddLeaders(int projectId, AddProjectLeaderDto dto);
        Task<AddProjectMemberResultDto> AddMembers(int projectId, AddProjectMemberDto dto);
        Task<ViewProjectMemberDto> Update(int projectId, int memberId, UpdateProjectMemberDto dto);
        Task<RemoveProjectMemberResultDto> Remove(int projectId, int memberId);
    }
}
