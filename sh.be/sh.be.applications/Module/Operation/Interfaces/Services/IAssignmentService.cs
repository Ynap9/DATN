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
        Task<BaseResponsePagingDto<ViewAssignmentTicketDto>> FindTicketPaging(int projectId, int documentTypeId, FindPagingAssignmentTicketDto dto);
        Task<AssignmentChangeResultDto> Revoke(int projectId, int documentTypeId, RevokeAssignmentDto dto);
        Task<AssignmentChangeResultDto> Transfer(int projectId, int documentTypeId, TransferAssignmentDto dto);
        Task<AssignmentChangeResultDto> RevokeQaOwnership(int projectId, int documentTypeId, RevokeQaOwnershipDto dto);
        Task<AssignmentChangeResultDto> TransferQaOwnership(int projectId, int documentTypeId, TransferQaOwnershipDto dto);
        Task<BaseResponsePagingDto<GetListMyAssignmentDto>> FindMyAssignments(FindMyAssignmentDto dto);
    }
}
