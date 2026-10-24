using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Dtos.ProjectDeadline
{
    public class FindPagingProjectDeadlineNotificationDto : BaseRequestPagingDto
    {
        public int? ProjectId { get; set; }
        public int? NotificationType { get; set; }
        public bool? IsSuccess { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
