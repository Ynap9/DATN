using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base.Interfaces;
using sh.be.domain.Base;
using sh.be.domain.Module.Core.Entities;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Base
{
    public class BaseService
    {
        public static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        public readonly IUnitOfWork _unitOfWork;
        public readonly IAuditLogRepository _auditLogRepository;
        public readonly IMapper _mapper;
        public readonly ILogger<BaseService> _logger;
        public readonly IRequestContext _requestContext;
        public readonly TimeProvider _timeProvider;

        public BaseService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<BaseService> logger, IRequestContext requestContext, TimeProvider timeProvider)
        {
            _unitOfWork = unitOfWork;
            _auditLogRepository = auditLogRepository;
            _mapper = mapper;
            _logger = logger;
            _requestContext = requestContext;
            _timeProvider = timeProvider;
        }

        protected string getCurrentUserId()
        {
            return _requestContext.UserId!;
        }

        protected DateTime GetVietnamTime()
        {
            return TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), VietnamTimeZone).DateTime;
        }

        protected void AddAuditLog(string action, string actionType, string content, object? oldData = null, object? newData = null, string? userId = null)
        {
            _auditLogRepository.Add(AuditLog.Create(
                userId ?? getCurrentUserId(),
                action,
                actionType,
                content,
                oldData,
                newData,
                _requestContext.Source ?? AuditLogConstants.SOURCE_SYSTEM,
                _requestContext.ContentType,
                GetVietnamTime()));
        }
    }
}
