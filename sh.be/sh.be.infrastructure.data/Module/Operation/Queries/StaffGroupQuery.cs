using AutoMapper;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Operation.Dtos.StaffGroup;
using sh.be.applications.Module.Operation.Interfaces.Queries;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Auth;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.infrastructure.data.Module.Operation.Queries
{
    public class StaffGroupQuery : BaseQuery, IStaffGroupQuery
    {
        public StaffGroupQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        public async Task<BaseResponsePagingDto<ViewStaffGroupDto>> FindPaging(FindPagingStaffGroupDto dto)
        {
            var query = _shDbContext.StaffGroups.AsNoTracking();
            if (!string.IsNullOrEmpty(dto.OwnerUserId))
            {
                query = query.Where(g => g.OwnerUserId == dto.OwnerUserId);
            }
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(g => g.Name.Contains(dto.Keyword) || (g.Description != null && g.Description.Contains(dto.Keyword)));
            }
            if (!string.IsNullOrEmpty(dto.Role))
            {
                query = query.Where(g => _shDbContext.StaffGroupMembers.Any(m => m.StaffGroupId == g.Id
                    && _shDbContext.UserRoles.Any(ur => ur.UserId == m.UserId
                        && _shDbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name == dto.Role))));
            }

            var totalCount = await query.CountAsync();

            var staffGroups = await query
                .OrderByDescending(g => g.CreatedDate)
                .ThenByDescending(g => g.Id)
                .Paging(dto)
                .Select(g => new ViewStaffGroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    OwnerUserId = g.OwnerUserId,
                    OwnerFullName = _shDbContext.Users.Where(u => u.Id == g.OwnerUserId).Select(u => u.FullName).FirstOrDefault(),
                    MemberCount = _shDbContext.StaffGroupMembers.Count(m => m.StaffGroupId == g.Id),
                    CreatedDate = g.CreatedDate,
                })
                .ToListAsync();

            var staffGroupIds = staffGroups.Select(g => g.Id).ToList();
            var staffGroupRoles = await (from m in _shDbContext.StaffGroupMembers
                                         join ur in _shDbContext.UserRoles on m.UserId equals ur.UserId
                                         join r in _shDbContext.Roles on ur.RoleId equals r.Id
                                         where staffGroupIds.Contains(m.StaffGroupId) && RoleConstants.EXCLUSIVE_STAFF_ROLES.Contains(r.Name)
                                         select new { m.StaffGroupId, r.Name }).Distinct().ToListAsync();
            var roleNamesByStaffGroup = staffGroupRoles.ToLookup(x => x.StaffGroupId, x => x.Name);
            foreach (var staffGroup in staffGroups)
            {
                staffGroup.Role = JoinStaffRoles(roleNamesByStaffGroup[staffGroup.Id]);
            }

            return new BaseResponsePagingDto<ViewStaffGroupDto>
            {
                Items = staffGroups,
                TotalItems = totalCount,
            };
        }

        public async Task<FindStaffGroupByIdDto?> FindById(int id)
        {
            var staffGroup = await _shDbContext.StaffGroups
                .AsNoTracking()
                .Where(g => g.Id == id)
                .Select(g => new FindStaffGroupByIdDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    OwnerUserId = g.OwnerUserId,
                    OwnerFullName = _shDbContext.Users.Where(u => u.Id == g.OwnerUserId).Select(u => u.FullName).FirstOrDefault(),
                    CreatedDate = g.CreatedDate,
                })
                .FirstOrDefaultAsync();
            if (staffGroup == null)
            {
                return null;
            }

            var members = await (from m in _shDbContext.StaffGroupMembers
                                 join u in _shDbContext.Users on m.UserId equals u.Id
                                 where m.StaffGroupId == id
                                 orderby u.FullName
                                 select new
                                 {
                                     u.Id,
                                     u.UserName,
                                     u.FullName,
                                     u.Email,
                                     m.CreatedDate,
                                     RoleNames = _shDbContext.UserRoles
                                         .Where(ur => ur.UserId == u.Id)
                                         .Join(_shDbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                                         .ToList(),
                                 }).AsNoTracking().ToListAsync();

            staffGroup.Members = members.Select(m => new StaffGroupMemberDto
            {
                UserId = m.Id,
                UserName = m.UserName ?? "",
                FullName = m.FullName,
                Email = m.Email ?? "",
                RoleNames = string.Join(", ", m.RoleNames.OrderBy(name => name)),
                AddedDate = m.CreatedDate,
            }).ToList();
            staffGroup.Role = JoinStaffRoles(members.SelectMany(m => m.RoleNames).Where(RoleConstants.EXCLUSIVE_STAFF_ROLES.Contains));
            return staffGroup;
        }

        public async Task<BaseResponsePagingDto<StaffGroupCandidateDto>> FindCandidates(FindStaffGroupCandidateDto dto)
        {
            var query = _shDbContext.Users
                .AsNoTracking()
                .Where(u => _shDbContext.UserRoles.Any(ur => ur.UserId == u.Id
                    && _shDbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name == dto.Role)));
            if (dto.ExcludeStaffGroupId.HasValue)
            {
                query = query.Where(u => !_shDbContext.StaffGroupMembers.Any(m => m.StaffGroupId == dto.ExcludeStaffGroupId && m.UserId == u.Id));
            }
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(u => u.UserName!.Contains(dto.Keyword)
                    || u.FullName.Contains(dto.Keyword)
                    || u.Email!.Contains(dto.Keyword));
            }

            var totalCount = await query.CountAsync();

            var candidates = await query
                .OrderBy(u => u.FullName)
                .ThenBy(u => u.Id)
                .Paging(dto)
                .Select(u => new StaffGroupCandidateDto
                {
                    UserId = u.Id,
                    UserName = u.UserName ?? "",
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    Role = dto.Role,
                })
                .ToListAsync();

            return new BaseResponsePagingDto<StaffGroupCandidateDto>
            {
                Items = candidates,
                TotalItems = totalCount,
            };
        }

        private static string? JoinStaffRoles(IEnumerable<string> roleNames)
        {
            var distinctRoleNames = roleNames.Distinct().OrderBy(name => name).ToList();
            return distinctRoleNames.Count == 0 ? null : string.Join(", ", distinctRoleNames);
        }
    }
}
