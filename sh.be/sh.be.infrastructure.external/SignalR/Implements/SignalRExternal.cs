using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base.Dtos.SignalR;
using sh.be.applications.Base.Interfaces.External.SignalR;
using sh.be.infrastructure.external.SignalR.Base;

namespace sh.be.infrastructure.external.SignalR.Implements
{
    public class SignalRExternal : ISignalRExternal
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly SignalRConnectionTracker _connectionTracker;
        private readonly ILogger<SignalRExternal> _logger;

        public SignalRExternal(IHubContext<NotificationHub> hubContext, SignalRConnectionTracker connectionTracker, ILogger<SignalRExternal> logger)
        {
            _hubContext = hubContext;
            _connectionTracker = connectionTracker;
            _logger = logger;
        }

        public SignalRConnectionDto CheckConnection()
        {
            return new SignalRConnectionDto
            {
                IsConnected = true,
                ConnectionCount = _connectionTracker.ConnectionCount,
            };
        }

        public int CountUserConnections(string userId)
        {
            return _connectionTracker.CountByUserId(userId);
        }

        public Task SendToUserAsync(string userId, string eventName, object payload)
        {
            _logger.LogInformation($"{nameof(SendToUserAsync)}, userId={userId}, eventName={eventName}");
            return _hubContext.Clients.User(userId).SendAsync(eventName, payload);
        }
    }
}
