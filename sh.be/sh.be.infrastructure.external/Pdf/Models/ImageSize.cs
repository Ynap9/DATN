namespace sh.be.infrastructure.external.Pdf.Models
{
    public class ImageSize
    {
        public int PixelWidth { get; set; }
        public int PixelHeight { get; set; }
        public double DpiX { get; set; }
        public double DpiY { get; set; }
        public bool HasDpiMetadata { get; set; }
        public double WidthPoints { get; set; }
        public double HeightPoints { get; set; }
    }
}
