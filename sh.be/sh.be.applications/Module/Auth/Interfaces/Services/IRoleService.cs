using sh.be.applications.Module.Auth.Dtos.Role;
using sh.be.shared.HttpRequest.BaseRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Auth.Interfaces.Services
{
    public interface IRoleService
    {
        List<GetListRoleDropDownDto> GetRoleDropDown();
        FindRoleByIdDto FindById(int id);
        BaseResponsePagingDto<GetListRoleDto> FindPaging(FindPagingRoleDto dto);
        List<GetRolePermissionTreeDto> GetPermissionTree(int roleId);
        Task UpdatePermission(UpdateRolePermissionDto dto);
        void Create(CreateRoleDto dto);
        void Delete(int id);
        void Update(int id, UpdateRoleDto dto);
    }
}
