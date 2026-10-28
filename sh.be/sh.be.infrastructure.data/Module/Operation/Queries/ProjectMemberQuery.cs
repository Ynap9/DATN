using AutoMapper;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Operation.Dtos.ProjectMember;
using sh.be.applications.Module.Operation.Interfaces.Queries;
using sh.be.domain.Module.Operation.ProjectMembers.Entities;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Operation;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.infrastructure.data.Module.Operation.Queries
{
    public class ProjectMemberQuery : BaseQuery, IProjectMemberQuery
    {
        public ProjectMemberQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        public async Task<BaseResponsePagingDto<ViewProjectMemberDto>> FindPaging(int projectId, FindPagingProjectMemberDto dto, string? leaderUserId)
        {
            var query = _shDbContext.ProjectMembers
                .AsNoTracking()
                .Where(m => m.ProjectId == projectId && m.DocumentTypeId == dto.DocumentTypeId);
            if (leaderUserId != null)
            {
                query = query.Where(m => (m.Role == RoleConstants.ROLE_LEADER && m.UserId == leaderUserId) || m.ManagedByUserId == leaderUserId);
            }
            else if (!string.IsNullOrEmpty(dto.ManagedByUserId))
            {
                query = query.Where(m => m.ManagedByUserId == dto.ManagedByUserId);
            }
            if (!string.IsNullOrEmpty(dto.Role))
            {
                query = query.Where(m => m.Role == dto.Role);
            }
            if (dto.IsActive.HasValue)
            {
                query = query.Where(m => m.IsActive == dto.IsActive);
            }
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(m => _shDbContext.Users.Any(u => u.Id == m.UserId && (u.UserName!.Contains(dto.Keyword) || u.FullName.Contains(dto.Keyword))));
            }

            var totalCount = await query.CountAsync();

            var members = await ToViewDtos(query
                .OrderBy(m => m.Role == RoleConstants.ROLE_LEADER ? 0 : m.Role == RoleConstants.ROLE_DATA ? 1 : 2)
                .ThenBy(m => _shDbContext.Users.Where(u => u.Id == m.UserId).Select(u => u.FullName).FirstOrDefault())
                .ThenBy(m => m.Id)
                .Paging(dto));

            return new BaseResponsePagingDto<ViewProjectMemberDto>
            {
                Items = members,
                TotalItems = totalCount,
            };
        }

        public Task<List<ViewProjectMemberDto>> FindByIds(IReadOnlyCollection<int> ids)
        {
            return ToViewDtos(_shDbContext.ProjectMembers
                .AsNoTracking()
                .Where(m => ids.Contains(m.Id))
                .OrderBy(m => m.Id));
        }

        public async Task<BaseResponsePagingDto<ProjectMemberCandidateDto>> FindCandidates(int projectId, FindProjectMemberCandidateDto dto)
        {
            var query = _shDbContext.Users
                .AsNoTracking()
                .Where(u => _shDbContext.UserRoles.Any(ur => ur.UserId == u.Id
                    && _shDbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name == dto.Role))
                    && !_shDbContext.ProjectMembers.Any(m => m.ProjectId == projectId && m.DocumentTypeId == dto.DocumentTypeId && m.UserId == u.Id));
            if (dto.StaffGroupId.HasValue)
            {
                query = query.Where(u => _shDbContext.StaffGroupMembers.Any(m => m.StaffGroupId == dto.StaffGroupId && m.UserId == u.Id));
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
                .Select(u => new ProjectMemberCandidateDto
                {
                    UserId = u.Id,
                    UserName = u.UserName ?? "",
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                })
                .ToListAsync();

            return new BaseResponsePagingDto<ProjectMemberCandidateDto>
            {
                Items = candidates,
                TotalItems = totalCount,
            };
        }

        public Task<Dictionary<string, string>> FindFullNames(IReadOnlyCollection<string> userIds)
        {
            return _shDbContext.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName);
        }

        private async Task<List<ViewProjectMemberDto>> ToViewDtos(IQueryable<ProjectMember> query)
        {
            var members = await query
                .Select(m => new ViewProjectMemberDto
                {
                    Id = m.Id,
                    ProjectId = m.ProjectId,
                    DocumentTypeId = m.DocumentTypeId,
                    UserId = m.UserId,
                    UserName = _shDbContext.Users.Where(u => u.Id == m.UserId).Select(u => u.UserName).FirstOrDefault() ?? "",
                    FullName = _shDbContext.Users.Where(u => u.Id == m.UserId).Select(u => u.FullName).FirstOrDefault(),
                    Role = m.Role,
                    ManagedByUserId = m.ManagedByUserId,
                    ManagedByFullName = _shDbContext.Users.Where(u => u.Id == m.ManagedByUserId).Select(u => u.FullName).FirstOrDefault(),
                    SourceStaffGroupId = m.SourceStaffGroupId,
                    IsActive = m.IsActive,
                    CreatedDate = m.CreatedDate,
                    HeldTicketCount = m.Role == RoleConstants.ROLE_DATA
                        ? _shDbContext.AssignmentTickets.Count(t => t.ProjectId == m.ProjectId && t.DocumentTypeId == m.DocumentTypeId
                            && t.AssigneeUserId == m.UserId && t.Status == AssignmentConst.STATUS_ACTIVE)
                        : m.Role == RoleConstants.ROLE_QA
                            ? _shDbContext.QaTickets.Count(t => t.ProjectId == m.ProjectId && t.DocumentTypeId == m.DocumentTypeId && t.QaUserId == m.UserId
                                && (t.Status == QaAssignmentConst.TICKET_STATUS_ACTIVE || t.Status == QaAssignmentConst.TICKET_STATUS_REJECTED))
                            : 0,
                })
                .ToListAsync();

            var staffGroupIds = members.Where(m => m.SourceStaffGroupId.HasValue).Select(m => m.SourceStaffGroupId!.Value).Distinct().ToList();
            var staffGroupNames = await _shDbContext.StaffGroups
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(g => staffGroupIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Name);
            foreach (var member in members.Where(m => m.SourceStaffGroupId.HasValue))
            {
                member.SourceStaffGroupName = staffGroupNames.GetValueOrDefault(member.SourceStaffGroupId!.Value);
            }
            return members;
        }
    }
}
