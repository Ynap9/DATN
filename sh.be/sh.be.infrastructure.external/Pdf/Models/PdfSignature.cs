namespace sh.be.infrastructure.external.Pdf.Models
{
    public class PdfSignature
    {
        public byte[] SignedContent { get; set; } = [];
        public byte[] Cms { get; set; } = [];
        public bool CoversWholeFile { get; set; }
    }
}
