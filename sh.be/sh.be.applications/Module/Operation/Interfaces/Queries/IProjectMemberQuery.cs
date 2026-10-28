using sh.be.applications.Module.Operation.Dtos.ProjectMember;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Operation.Interfaces.Queries
{
    public interface IProjectMemberQuery
    {
        Task<BaseResponsePagingDto<ViewProjectMemberDto>> FindPaging(int projectId, FindPagingProjectMemberDto dto, string? leaderUserId);
        Task<List<ViewProjectMemberDto>> FindByIds(IReadOnlyCollection<int> ids);
        Task<BaseResponsePagingDto<ProjectMemberCandidateDto>> FindCandidates(int projectId, FindProjectMemberCandidateDto dto);
        Task<Dictionary<string, string>> FindFullNames(IReadOnlyCollection<string> userIds);
    }
}
