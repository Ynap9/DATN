namespace sh.be.infrastructure.external.DigitalSignature.Models
{
    public class PdfAnnotationPlan
    {
        public int NextObjectNumber { get; set; }
        public List<PdfAnnotation> Annotations { get; } = new();
        public List<PdfIndirectObject> AppearanceObjects { get; } = new();
    }
}
