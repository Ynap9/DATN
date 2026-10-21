namespace sh.be.applications.Base.Dtos.Signing
{
    public class PdfPreparedSignatureDto
    {
        public byte[] Bytes { get; set; } = [];
        public int ContentsHexStart { get; set; }
        public int ContentsHexLength { get; set; }
        public byte[] SignedAttributes { get; set; } = [];
        public DateTime SignedAt { get; set; }
    }
}
