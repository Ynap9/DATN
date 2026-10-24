using sh.be.applications.Base.Dtos.SignalR;

namespace sh.be.applications.Base.Interfaces.External.SignalR
{
    public interface ISignalRExternal
    {
        SignalRConnectionDto CheckConnection();
        int CountUserConnections(string userId);
        Task SendToUserAsync(string userId, string eventName, object payload);
    }
}
