using sh.be.applications.Base.Dtos.SignalR;

namespace sh.be.applications.Module.Core.Interfaces.Services
{
    public interface ISignalRService
    {
        SignalRConnectionDto CheckConnection();
        Task<SignalRPingDto> Ping();
    }
}
