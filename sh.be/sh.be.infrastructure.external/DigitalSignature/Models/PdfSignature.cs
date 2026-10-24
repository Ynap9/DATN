namespace sh.be.infrastructure.external.DigitalSignature.Models
{
    public class PdfSignature
    {
        public byte[] SignedContent { get; set; } = [];
        public byte[] Cms { get; set; } = [];
        public bool CoversWholeFile { get; set; }
    }
}
