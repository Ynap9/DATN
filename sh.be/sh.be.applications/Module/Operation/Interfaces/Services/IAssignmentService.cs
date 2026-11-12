using sh.be.applications.Module.Operation.Dtos.Assignment;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Operation.Interfaces.Services
{
    public interface IAssignmentService
    {
        Task<BaseResponsePagingDto<AssignmentNodeDto>> FindTree(int projectId, int documentTypeId, FindAssignmentTreeDto dto);
        Task<BaseResponsePagingDto<AssignmentNodeDto>> FindProfiles(int projectId, int documentTypeId, FindAssignmentProfilesDto dto);
        Task<AssignmentPreviewDto> Preview(int projectId, int documentTypeId, AssignmentRequestDto dto);
        Task<ViewAssignmentTicketDto> Assign(int projectId, int documentTypeId, AssignmentRequestDto dto);
        Task<ViewAssignmentTicketDto> FindTicketById(int projectId, int documentTypeId, int ticketId);
        Task<BaseResponsePagingDto<GetListMyAssignmentDto>> FindMyAssignments(FindMyAssignmentDto dto);
    }
}
