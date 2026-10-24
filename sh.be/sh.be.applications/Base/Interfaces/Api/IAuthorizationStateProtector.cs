namespace sh.be.applications.Base.Interfaces.Api
{
    public interface IAuthorizationStateProtector
    {
        string CreateState(string? userId);
        string? ReadUserId(string? state);
    }
}
