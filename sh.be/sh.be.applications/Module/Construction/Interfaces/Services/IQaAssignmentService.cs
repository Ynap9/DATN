using sh.be.applications.Module.Construction.Dtos.QaAssignment;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Interfaces.Services
{
    public interface IQaAssignmentService
    {
        Task<ViewQaSamplingConfigDto> FindConfig(int projectId);
        Task UpdateConfig(UpdateQaSamplingConfigDto dto);
        Task<List<GetListQaReviewerModeDto>> FindReviewerModes(int projectId);
        Task UpdateReviewerMode(UpdateQaReviewerModeDto dto);
        Task<FindQaTicketByIdDto> CreateTicket(CreateQaTicketDto dto);
        Task<BaseResponsePagingDto<GetListQaTicketDto>> FindTicketPaging(FindPagingQaTicketDto dto);
        Task<FindQaTicketByIdDto> FindTicketById(int id);
        Task<FindQaTicketByIdDto> CancelTicket(int id);
    }
}
