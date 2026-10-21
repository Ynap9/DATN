namespace sh.be.applications.Base.Dtos.Signing
{
    public class PdfSigningPositionDto
    {
        public int Kind { get; set; }
        public int PageNumber { get; set; }
        public double XRatio { get; set; }
        public double YRatio { get; set; }
        public double WidthRatio { get; set; }
        public double HeightRatio { get; set; }
    }
}
