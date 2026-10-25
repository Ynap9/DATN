using AutoMapper;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Construction.Dtos.ProjectDeadline;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.infrastructure.data.Module.Construction.Queries
{
    public class ProjectDeadlineQuery : BaseQuery, IProjectDeadlineQuery
    {
        public ProjectDeadlineQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        public async Task<BaseResponsePagingDto<GetListProjectDeadlineDto>> FindPaging(FindPagingProjectDeadlineDto dto, DateTime today, int warningDays)
        {
            var warningEndExclusive = today.AddDays(warningDays + 1);
            var query = _shDbContext.Projects
                .AsNoTracking()
                .Where(p => ProjectConst.DEADLINE_STATUSES.Contains(p.Status) && p.EndedAt != null
                    && (p.Status == ProjectConst.EXPIRED || p.EndedAt < warningEndExclusive));
            if (dto.NotificationType == ProjectDeadlineConst.NOTIFICATION_EXPIRED)
            {
                query = query.Where(p => p.Status == ProjectConst.EXPIRED || p.EndedAt < today);
            }
            if (dto.NotificationType == ProjectDeadlineConst.NOTIFICATION_WARNING)
            {
                query = query.Where(p => p.Status != ProjectConst.EXPIRED && p.EndedAt >= today);
            }
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(p => p.Name.Contains(dto.Keyword));
            }

            var totalItems = await query.CountAsync();
            var projects = await query
                .OrderBy(p => p.EndedAt)
                .ThenBy(p => p.Id)
                .Paging(dto)
                .Select(p => new GetListProjectDeadlineDto
                {
                    ProjectId = p.Id,
                    Name = p.Name,
                    Status = p.Status,
                    StartedAt = p.StartedAt,
                    EndedAt = p.EndedAt,
                    DaysLeft = EF.Functions.DateDiffDay(today, p.EndedAt!.Value),
                    DeadlineType = p.Status == ProjectConst.EXPIRED || p.EndedAt < today
                        ? ProjectDeadlineConst.NOTIFICATION_EXPIRED
                        : ProjectDeadlineConst.NOTIFICATION_WARNING,
                    LastNotifiedDate = _shDbContext.ProjectDeadlineNotifications
                        .Where(n => n.ProjectId == p.Id && n.IsSuccess)
                        .Max(n => n.CreatedDate),
                })
                .ToListAsync();

            return new BaseResponsePagingDto<GetListProjectDeadlineDto>
            {
                Items = projects,
                TotalItems = totalItems,
            };
        }

        public async Task<BaseResponsePagingDto<GetListProjectDeadlineNotificationDto>> FindNotificationPaging(FindPagingProjectDeadlineNotificationDto dto)
        {
            var query = _shDbContext.ProjectDeadlineNotifications.AsNoTracking();
            if (dto.ProjectId.HasValue)
            {
                query = query.Where(n => n.ProjectId == dto.ProjectId.Value);
            }
            if (dto.NotificationType.HasValue)
            {
                query = query.Where(n => n.NotificationType == dto.NotificationType.Value);
            }
            if (dto.IsSuccess.HasValue)
            {
                query = query.Where(n => n.IsSuccess == dto.IsSuccess.Value);
            }
            if (dto.FromDate.HasValue)
            {
                var fromDate = dto.FromDate.Value.Date;
                query = query.Where(n => n.CreatedDate >= fromDate);
            }
            if (dto.ToDate.HasValue)
            {
                var toDateExclusive = dto.ToDate.Value.Date.AddDays(1);
                query = query.Where(n => n.CreatedDate < toDateExclusive);
            }
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(n => n.ProjectName.Contains(dto.Keyword) || n.ToAddress.Contains(dto.Keyword));
            }

            var totalItems = await query.CountAsync();
            var notifications = await query
                .OrderByDescending(n => n.CreatedDate)
                .ThenByDescending(n => n.Id)
                .Paging(dto)
                .Select(n => new GetListProjectDeadlineNotificationDto
                {
                    Id = n.Id,
                    ProjectId = n.ProjectId,
                    ProjectName = n.ProjectName,
                    NotificationType = n.NotificationType,
                    EndedAt = n.EndedAt,
                    DaysLeft = n.DaysLeft,
                    ToAddress = n.ToAddress,
                    Subject = n.Subject,
                    IsSuccess = n.IsSuccess,
                    ErrorMessage = n.ErrorMessage,
                    CreatedDate = n.CreatedDate,
                })
                .ToListAsync();

            return new BaseResponsePagingDto<GetListProjectDeadlineNotificationDto>
            {
                Items = notifications,
                TotalItems = totalItems,
            };
        }
    }
}
