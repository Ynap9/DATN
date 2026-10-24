using sh.be.applications.Base.Dtos.DigitalSignature;
using System.Security.Cryptography.X509Certificates;

namespace sh.be.applications.Base.Interfaces.External.DigitalSignature
{
    public interface IPdfSignerExternal
    {
        PdfSigningContextDto CreateContext(X509Certificate2 certificate, PdfSigningOptionsDto options);
        bool HasSignature(byte[] pdf);
        PdfPreparedSignatureDto Prepare(byte[] pdf, PdfSigningContextDto context, DateTime signedAt, DateTime signedAtUtc);
        byte[] Complete(PdfPreparedSignatureDto prepared, byte[] signature, byte[] timestampToken, PdfSigningContextDto context);
    }
}
