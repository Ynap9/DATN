using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Auth.Dtos.User;
using sh.be.applications.Module.Auth.Interfaces.Queries;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.infrastructure.data.Module.Auth.Queries
{
    public class UserQuery : IUserQuery
    {
        private readonly ShDbContext _shDbContext;

        public UserQuery(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public ViewMeDto? GetMe(string userId)
        {
            return _shDbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => new ViewMeDto
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "",
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    Roles = _shDbContext.UserRoles
                            .Where(ur => ur.UserId == u.Id)
                            .Join(_shDbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r)
                            .Select(r => new UserRoleDto
                            {
                                Id = r.Id,
                                Name = r.Name,
                                Description = r.Description,
                                Permissions = _shDbContext.RolePermissions
                                .Where(rp => rp.RoleId == r.Id)
                                .Join(_shDbContext.Permissions, rp => rp.PermissionKey, p => p.Key, (rp, p) => p)
                                .Select(P => new UserPermissionDto
                                {
                                    Key = P.Key,
                                    Description = P.Description,
                                    ParentKey = P.ParentKey,
                                    Module = P.Module
                                }).ToList()
                            }).ToList()
                }).FirstOrDefault();
        }

        public async Task<BaseResponsePagingDto<ViewUserDto>> FindPaging(FindPagingUserDto dto)
        {
            var query = _shDbContext.Users.AsNoTracking();
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(u => u.UserName!.Contains(dto.Keyword)
                                    || u.FullName!.Contains(dto.Keyword)
                                    || u.Email!.Contains(dto.Keyword)
                                    ||_shDbContext.UserRoles.Any(ur => ur.UserId == u.Id
                                    && _shDbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name!.Contains(dto.Keyword)))
                );
            }

            var totalCount = await query.CountAsync();

            var user = await query
                .Paging(dto)
                .AsNoTracking()
                .ToListAsync();
            var userIds = user.Select(u=> u.Id).ToList();

            var roles = await _shDbContext.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(_shDbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Id, r.Name })
                .ToListAsync();
            var roleByUser = roles.ToLookup( x=> x.UserId );
            var items = user.Select( u => new ViewUserDto
            {
                Id = u.Id,
                UserName = u.UserName ?? " ",
                FullName = u.FullName,
                Email = u.Email ?? " ",
                Roles = roleByUser[u.Id]
                        .Select(r => new UserRolePaging
                        {
                            Id = r.Id,
                            Name = r.Name,
                        }).ToList(),
            }).ToList();
            return new BaseResponsePagingDto<ViewUserDto>
            {
                Items = items,
                TotalItems = totalCount,
            };
        }
    }
}
