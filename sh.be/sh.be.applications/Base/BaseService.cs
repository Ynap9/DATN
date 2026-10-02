using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.domain.Module.Core;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Core;
using sh.be.shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace sh.be.applications.Base
{
    public class BaseService
    {
        public static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        public readonly ShDbContext _shDbContext;
        public readonly IMapper _mapper;
        public readonly ILogger<BaseService> _logger;
        public readonly IHttpContextAccessor _httpContextAccessor;

        public BaseService(ShDbContext shDbContext, IMapper mapper, ILogger<BaseService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _shDbContext = shDbContext;
            _mapper = mapper;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        protected string getCurrentUserId()
        {
            var data = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(data))
            {
                data = _httpContextAccessor.HttpContext?.User.FindFirstValue(Claims.Subject);
            }
            //_logger.LogInformation($"getCurrentUserId: {data}");
            return data!;
        }

        protected bool IsSuperAdmin()
        {
            var roles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).ToList();
            var isSuperAdmin = roles?.Any(r => r.Value == RoleConstants.ROLE_SUPER_ADMIN) ?? false;
            return isSuperAdmin;
        }
        protected static DateTime GetVietnamTime()
        {
            return DateTimeUtils.GetVietnamTime();
        }

        protected void AddAuditLog(string action, string actionType, string content, object? oldData = null, object? newData = null, string? userId = null)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            _shDbContext.AuditLogs.Add(new AuditLog
            {
                UserId = userId ?? getCurrentUserId(),
                Action = action,
                ActionType = actionType,
                Content = content,
                OldData = AuditLogUtils.ToJson(oldData),
                NewData = AuditLogUtils.ToJson(newData),
                Source = request == null ? AuditLogConstants.SOURCE_SYSTEM : $"{request.Method} {request.Path}",
                ContentType = request?.ContentType,
                CreatedDate = GetVietnamTime(),
            });
        }
    }
}
