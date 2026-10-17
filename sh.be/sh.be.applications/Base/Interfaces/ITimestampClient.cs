namespace sh.be.applications.Base.Interfaces
{
    public interface ITimestampClient
    {
        Task<byte[]> RequestTokenAsync(byte[] signature, CancellationToken cancellationToken);
        DateTime? ReadTimeUtc(byte[] token);
    }
}
