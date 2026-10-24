namespace sh.be.applications.Base.Interfaces.External.DigitalSignature
{
    public interface ITimestampExternal
    {
        Task<byte[]> RequestTokenAsync(byte[] signature, CancellationToken cancellationToken);
        DateTime? ReadTimeUtc(byte[] token);
    }
}
