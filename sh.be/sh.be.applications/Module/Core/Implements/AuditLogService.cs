using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Module.Core.Dtos.AuditLog;
using sh.be.applications.Module.Core.Interfaces;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Core.Implements
{
    public class AuditLogService : BaseService, IAuditLogService
    {
        public AuditLogService(ShDbContext shDbContext, IMapper mapper, ILogger<AuditLogService> logger, IHttpContextAccessor httpContextAccessor
        ) : base(shDbContext, mapper, logger, httpContextAccessor)
        {
        }

        public async Task<BaseResponsePagingDto<ViewAuditLogDto>> FindPaging(FindPagingAuditLogDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, dto={JsonSerializer.Serialize(dto)}");
            var query = _shDbContext.AuditLogs.AsNoTracking();
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(a => a.Action.Contains(dto.Keyword)
                                    || a.Content.Contains(dto.Keyword)
                                    || _shDbContext.Users.Any(u => u.Id == a.UserId
                                    && (u.UserName!.Contains(dto.Keyword) || u.FullName.Contains(dto.Keyword)))
                );
            }
            if (!string.IsNullOrEmpty(dto.UserId))
            {
                query = query.Where(a => a.UserId == dto.UserId);
            }
            if (!string.IsNullOrEmpty(dto.ActionType))
            {
                query = query.Where(a => a.ActionType == dto.ActionType);
            }
            if (!string.IsNullOrEmpty(dto.Source))
            {
                query = query.Where(a => a.Source.Contains(dto.Source));
            }
            if (dto.FromDate != null)
            {
                query = query.Where(a => a.CreatedDate >= dto.FromDate);
            }
            if (dto.ToDate != null)
            {
                query = query.Where(a => a.CreatedDate <= dto.ToDate);
            }

            var totalCount = await query.CountAsync();

            var auditLogs = await query
                .OrderByDescending(a => a.CreatedDate)
                .ThenByDescending(a => a.Id)
                .Paging(dto)
                .Select(a => new ViewAuditLogDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    Action = a.Action,
                    ActionType = a.ActionType,
                    //Content = a.Content,
                    Source = a.Source,
                    ContentType = a.ContentType,
                    CreatedDate = a.CreatedDate,
                })
                .ToListAsync();

            var userIds = auditLogs.Where(a => a.UserId != null).Select(a => a.UserId).Distinct().ToList();
            var users = await _shDbContext.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName, u.FullName })
                .ToDictionaryAsync(u => u.Id);

            foreach (var auditLog in auditLogs)
            {
                if (auditLog.UserId != null && users.TryGetValue(auditLog.UserId, out var user))
                {
                    auditLog.UserName = user.UserName;
                    auditLog.FullName = user.FullName;
                }
            }

            return new BaseResponsePagingDto<ViewAuditLogDto>
            {
                Items = auditLogs,
                TotalItems = totalCount,
            };
        }

        public async Task<FindAuditLogByIdDto> FindById(long id)
        {
            _logger.LogInformation($"{nameof(FindById)}, id={id}");
            var auditLog = await _shDbContext.AuditLogs
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => new FindAuditLogByIdDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserName = _shDbContext.Users.Where(u => u.Id == a.UserId).Select(u => u.UserName).FirstOrDefault(),
                    FullName = _shDbContext.Users.Where(u => u.Id == a.UserId).Select(u => u.FullName).FirstOrDefault(),
                    Action = a.Action,
                    ActionType = a.ActionType,
                    Content = a.Content,
                    OldData = a.OldData,
                    NewData = a.NewData,
                    Source = a.Source,
                    ContentType = a.ContentType,
                    CreatedDate = a.CreatedDate,
                }).FirstOrDefaultAsync();
            if (auditLog == null)
            {
                throw new UserFriendlyException(ErrorCodes.CoreErrorAuditLogNotFound);
            }
            return auditLog;
        }

        public async Task Create(CreateAuditLogDto dto)
        {
            _logger.LogInformation($"{nameof(Create)}, action={dto.Action}, actionType={dto.ActionType}, userId={dto.UserId}");
            AddAuditLog(dto.Action, dto.ActionType, dto.Content, dto.OldData, dto.NewData, dto.UserId);
            await _shDbContext.SaveChangesAsync();
        }
    }
}
