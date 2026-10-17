namespace sh.be.applications.Module.DigitalSignature.Dtos.Template
{
    public class TemplatePositionDto
    {
        public int Kind { get; set; }
        public int PageNumber { get; set; } = 1;
        public double XRatio { get; set; }
        public double YRatio { get; set; }
        public double WidthRatio { get; set; }
        public double HeightRatio { get; set; }
    }
}
