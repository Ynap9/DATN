namespace sh.be.applications.Base.Interfaces
{
    public interface IAuthorizationStateProtector
    {
        string CreateState(string? userId);
        string? ReadUserId(string? state);
    }
}
