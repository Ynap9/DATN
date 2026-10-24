using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.SignalR;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Base.Interfaces.External.SignalR;
using sh.be.applications.Module.Core.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.SignalR;

namespace sh.be.applications.Module.Core.Implements
{
    public class SignalRService : BaseService, ISignalRService
    {
        private readonly ISignalRExternal _signalRExternal;

        public SignalRService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<SignalRService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            ISignalRExternal signalRExternal
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _signalRExternal = signalRExternal;
        }

        public SignalRConnectionDto CheckConnection()
        {
            _logger.LogInformation($"{nameof(CheckConnection)}");
            return _signalRExternal.CheckConnection();
        }

        public async Task<SignalRPingDto> Ping()
        {
            _logger.LogInformation($"{nameof(Ping)}");
            var currentUserId = getCurrentUserId();
            await _signalRExternal.SendToUserAsync(currentUserId, SignalRConst.EVENT_PING, new SignalRPingMessageDto
            {
                Message = SignalRConst.PING_MESSAGE,
                SentDate = GetVietnamTime(),
            });

            return new SignalRPingDto
            {
                UserId = currentUserId,
                ConnectionCount = _signalRExternal.CountUserConnections(currentUserId),
            };
        }
    }
}
