using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Module.Core.Dtos.AuditLog;
using sh.be.applications.Module.Core.Interfaces.Services;
using sh.be.applications.Module.Core.Interfaces.Queries;
using sh.be.domain.Base;
using sh.be.domain.Module.Core.Repositories;
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
        private readonly IAuditLogQuery _auditLogQuery;

        public AuditLogService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<AuditLogService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IAuditLogQuery auditLogQuery
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _auditLogQuery = auditLogQuery;
        }

        public async Task<BaseResponsePagingDto<ViewAuditLogDto>> FindPaging(FindPagingAuditLogDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, dto={JsonSerializer.Serialize(dto)}");
            return await _auditLogQuery.FindPaging(dto);
        }

        public async Task<FindAuditLogByIdDto> FindById(long id)
        {
            _logger.LogInformation($"{nameof(FindById)}, id={id}");
            var auditLog = await _auditLogQuery.FindById(id);
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
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
