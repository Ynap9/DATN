using sh.be.applications.Module.Construction.Dtos.ProjectDeadline;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Interfaces.Services
{
    public interface IProjectDeadlineService
    {
        Task<BaseResponsePagingDto<GetListProjectDeadlineDto>> FindPaging(FindPagingProjectDeadlineDto dto);
        Task<ViewRunDeadlineCheckDto> Run();
        Task<ViewDeadlineCheckStatusDto> FindStatus();
        Task<BaseResponsePagingDto<GetListProjectDeadlineNotificationDto>> FindNotificationPaging(FindPagingProjectDeadlineNotificationDto dto);
    }
}
