using sh.be.applications.Module.Construction.Dtos.QaAssignment;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Interfaces.Queries
{
    public interface IQaAssignmentQuery
    {
        Task<List<GetListQaReviewerModeDto>> FindReviewerModes(int projectId);
        Task<BaseResponsePagingDto<GetListQaTicketDto>> FindTicketPaging(FindPagingQaTicketDto dto, string? viewerUserId);
        Task<FindQaTicketByIdDto?> FindTicketById(int id);
    }
}
