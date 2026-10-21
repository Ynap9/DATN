using sh.be.applications.Base.Dtos.Signing;
using System.Security.Cryptography.X509Certificates;

namespace sh.be.applications.Base.Interfaces
{
    public interface IPdfSigner
    {
        PdfSigningContextDto CreateContext(X509Certificate2 certificate, PdfSigningOptionsDto options);
        bool HasSignature(byte[] pdf);
        PdfPreparedSignatureDto Prepare(byte[] pdf, PdfSigningContextDto context, DateTime signedAt, DateTime signedAtUtc);
        byte[] Complete(PdfPreparedSignatureDto prepared, byte[] signature, byte[] timestampToken, PdfSigningContextDto context);
    }
}
