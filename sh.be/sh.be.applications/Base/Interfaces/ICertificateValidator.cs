using System.Security.Cryptography.X509Certificates;

namespace sh.be.applications.Base.Interfaces
{
    public interface ICertificateValidator
    {
        X509Certificate2 LoadTrusted(string base64, DateTime verificationTimeUtc);
        bool IsTrusted(X509Certificate2 certificate, DateTime verificationTimeUtc);
    }
}
