using sh.be.applications.Module.Operation.Dtos.Assignment;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Operation.Interfaces.Queries
{
    public interface IAssignmentQuery
    {
        Task<BaseResponsePagingDto<AssignmentNodeDto>> FindBoxNodes(int projectId, int documentTypeId, FindAssignmentTreeDto dto);
        Task<BaseResponsePagingDto<AssignmentNodeDto>> FindProfileNodes(int projectId, int documentTypeId, FindAssignmentProfilesDto dto);
        Task<ViewAssignmentTicketDto?> FindTicketById(int ticketId);
        Task<BaseResponsePagingDto<GetListMyAssignmentDto>> FindMyAssignments(string userId, FindMyAssignmentDto dto);
        Task<Dictionary<string, string>> FindFullNames(List<string> userIds);
    }
}
