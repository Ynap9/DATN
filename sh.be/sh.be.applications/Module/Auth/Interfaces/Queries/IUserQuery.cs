using sh.be.applications.Module.Auth.Dtos.User;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Auth.Interfaces.Queries
{
    public interface IUserQuery
    {
        ViewMeDto? GetMe(string userId);
        Task<BaseResponsePagingDto<ViewUserDto>> FindPaging(FindPagingUserDto dto);
        Task<List<string>> GetEmailsByRoleNamesAsync(IReadOnlyCollection<string> roleNames);
    }
}
