namespace sh.be.infrastructure.external.Pdf.Models
{
    public class PdfAnnotation
    {
        public int Number { get; set; }
        public int PageObjectNumber { get; set; }
        public PdfRect Rect { get; set; } = new();
        public int? AppearanceObjectNumber { get; set; }
        public bool IsSignature { get; set; }
    }
}
