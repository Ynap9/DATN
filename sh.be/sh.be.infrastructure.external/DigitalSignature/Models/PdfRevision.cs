namespace sh.be.infrastructure.external.DigitalSignature.Models
{
    public class PdfRevision
    {
        public byte[] Bytes { get; set; } = [];
        public string Text { get; set; } = String.Empty;
        public long LastXrefOffset { get; set; }
        public bool LastXrefIsStream { get; set; }
        public int RootObjectNumber { get; set; }
        public int Size { get; set; }
        public string? IdRaw { get; set; }
        public string? InfoRaw { get; set; }
        public Dictionary<int, PdfObjectLocation> Objects { get; } = new();
    }
}
