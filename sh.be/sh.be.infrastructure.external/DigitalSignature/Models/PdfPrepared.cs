namespace sh.be.infrastructure.external.DigitalSignature.Models
{
    public class PdfPrepared
    {
        public byte[] Bytes { get; set; } = [];
        public byte[] SignedContent { get; set; } = [];
        public int ContentsHexStart { get; set; }
        public int ContentsHexLength { get; set; }
    }
}
