namespace sh.be.applications.Base.Interfaces.External.Ai
{
    public interface ISecretProtectorExternal
    {
        string Protect(string plainText, string purpose);
        string? TryUnprotect(string cipherText, string purpose);
    }
}
