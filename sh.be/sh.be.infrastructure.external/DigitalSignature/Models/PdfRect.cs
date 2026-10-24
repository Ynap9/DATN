namespace sh.be.infrastructure.external.DigitalSignature.Models
{
    public class PdfRect
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Right => X + Width;
        public double Top => Y + Height;
    }
}
