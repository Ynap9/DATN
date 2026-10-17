using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace sh.be.applications.Base.Dtos.Signing
{
    public class PdfSigningContextDto
    {
        public X509Certificate2 Certificate { get; set; } = null!;
        public IReadOnlyList<X509Certificate2> Chain { get; set; } = [];
        public string SignerName { get; set; } = String.Empty;
        public byte[] SigningCertificateV2 { get; set; } = [];
        public PdfSigningOptionsDto Options { get; set; } = new();
        public ConcurrentDictionary<string, byte[]> AppearanceCache { get; } = new();
    }
}
