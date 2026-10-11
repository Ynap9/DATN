namespace sh.be.applications.Base.Interfaces
{
    public interface IRequestContext
    {
        string? UserId { get; }
        string? Source { get; }
        string? ContentType { get; }
    }
}
