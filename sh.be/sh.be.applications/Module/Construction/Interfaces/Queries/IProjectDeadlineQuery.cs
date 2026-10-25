using sh.be.applications.Module.Construction.Dtos.ProjectDeadline;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Interfaces.Queries
{
    public interface IProjectDeadlineQuery
    {
        Task<BaseResponsePagingDto<GetListProjectDeadlineDto>> FindPaging(FindPagingProjectDeadlineDto dto, DateTime today, int warningDays);
        Task<BaseResponsePagingDto<GetListProjectDeadlineNotificationDto>> FindNotificationPaging(FindPagingProjectDeadlineNotificationDto dto);
    }
}
