using sh.be.applications.Module.Auth.Dtos.User;
using sh.be.shared.HttpRequest.BaseRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Auth.Interfaces
{
    public interface IUserService
    {
        ViewMeDto GetMe();
        Task<BaseResponsePagingDto<ViewUserDto>> FindPaging(FindPagingUserDto dto);
        Task Create(CreateUserDto dto);
        Task Update(UpdateUserDto dto);
        Task Delete(string userId);
        Task ChangePassword(ChangePasswordDto dto);
        Task UpdateRoleUser(UpdateRoleUserDto dto);
    }
}
