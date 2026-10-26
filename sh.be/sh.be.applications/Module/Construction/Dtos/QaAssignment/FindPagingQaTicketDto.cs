using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Dtos.QaAssignment
{
    public class FindPagingQaTicketDto : BaseRequestPagingDto
    {
        public int? ProjectId { get; set; }
        public int? DocumentTypeId { get; set; }
        public string? QaUserId { get; set; }
        public string? Mode { get; set; }
        public string? Status { get; set; }
    }
}
