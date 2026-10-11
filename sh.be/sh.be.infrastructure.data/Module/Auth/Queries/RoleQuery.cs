using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Auth.Dtos.Role;
using sh.be.applications.Module.Auth.Interfaces.Queries;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.infrastructure.data.Module.Auth.Queries
{
    public class RoleQuery : IRoleQuery
    {
        private readonly ShDbContext _shDbContext;

        public RoleQuery(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public List<GetListRoleDropDownDto> GetRoleDropDown()
        {
            return _shDbContext.Roles
                .Select(r => new GetListRoleDropDownDto
                {
                    Id = r.Id,
                    Name = r.Name,
                }).ToList();
        }

        public FindRoleByIdDto? FindById(int id)
        {
            return _shDbContext.Roles
                .Where(r => r.Id == id)
                .AsNoTracking()
                .Select(r => new FindRoleByIdDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                }).FirstOrDefault();
        }

        public BaseResponsePagingDto<GetListRoleDto> FindPaging(FindPagingRoleDto dto)
        {
            var query = _shDbContext.Roles.AsNoTracking();
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(r => r.Name!.Contains(dto.Keyword));
            }

            var totalCount = query.Count();
            var roles = query
                .Paging(dto)
                .AsNoTracking()
                .ToList();

            var items = roles.Select(r => new GetListRoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
            });

            return new BaseResponsePagingDto<GetListRoleDto>
            {
                Items = items,
                TotalItems = totalCount,
            };
        }
    }
}
