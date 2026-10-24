using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Dtos.ProjectDeadline
{
    public class FindPagingProjectDeadlineDto : BaseRequestPagingDto
    {
        public int? NotificationType { get; set; }
    }
}
