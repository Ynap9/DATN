using sh.be.applications.Module.Auth.Dtos.Role;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Auth.Interfaces.Queries
{
    public interface IRoleQuery
    {
        List<GetListRoleDropDownDto> GetRoleDropDown();
        FindRoleByIdDto? FindById(int id);
        BaseResponsePagingDto<GetListRoleDto> FindPaging(FindPagingRoleDto dto);
    }
}
