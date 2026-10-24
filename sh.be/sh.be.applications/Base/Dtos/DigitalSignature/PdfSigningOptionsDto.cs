namespace sh.be.applications.Base.Dtos.DigitalSignature
{
    public class PdfSigningOptionsDto
    {
        public string? SigningReason { get; set; }
        public string? SigningLocation { get; set; }
        public bool IsShowDigitalSignature { get; set; }
        public bool IsDigitalSignatureInImage { get; set; }
        public byte[]? StampImage { get; set; }
        public byte[]? WetSignatureImage { get; set; }
        public int StampIntensity { get; set; }
        public int WetSignatureIntensity { get; set; }
        public int WetSignatureStrokeWidth { get; set; }
        public string DigitalSignatureColor { get; set; } = String.Empty;
        public string? WetSignatureColor { get; set; }
        public string DigitalSignatureFont { get; set; } = String.Empty;
        public double DigitalSignatureFontSize { get; set; }
        public List<PdfSigningPositionDto> Positions { get; set; } = new();
    }
}
