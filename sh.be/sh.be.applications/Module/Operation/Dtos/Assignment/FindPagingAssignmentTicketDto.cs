using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Operation.Dtos.Assignment
{
    public class FindPagingAssignmentTicketDto : BaseRequestPagingDto
    {
        public string? Role { get; set; }
        public string? AssigneeUserId { get; set; }
        public int? Status { get; set; }
    }
}
