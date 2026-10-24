namespace sh.be.infrastructure.external.DigitalSignature.Models
{
    public class PdfIndirectObject
    {
        public int Number { get; set; }
        public string DictText { get; set; } = String.Empty;
        public byte[]? StreamData { get; set; }
    }
}
