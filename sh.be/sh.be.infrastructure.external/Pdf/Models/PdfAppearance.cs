namespace sh.be.infrastructure.external.Pdf.Models
{
    public class PdfAppearance
    {
        public int FormObjectNumber { get; set; }
        public List<PdfIndirectObject> Objects { get; } = new();
    }
}
