using Microsoft.AspNetCore.SignalR;

namespace sh.be.infrastructure.external.SignalR.Base
{
    public class NotificationHub : Hub
    {
        private readonly SignalRConnectionTracker _connectionTracker;

        public NotificationHub(SignalRConnectionTracker connectionTracker)
        {
            _connectionTracker = connectionTracker;
        }

        public override async Task OnConnectedAsync()
        {
            _connectionTracker.Add(Context.ConnectionId, Context.UserIdentifier);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _connectionTracker.Remove(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
