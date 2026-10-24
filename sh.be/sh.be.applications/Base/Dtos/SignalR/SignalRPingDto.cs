namespace sh.be.applications.Base.Dtos.SignalR
{
    public class SignalRPingDto
    {
        public string UserId { get; set; } = String.Empty;
        public int ConnectionCount { get; set; }
    }
}
