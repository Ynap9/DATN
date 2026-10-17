namespace sh.be.applications.Module.DigitalSignature.Dtos.Template
{
    public class FindTemplateByIdDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public string? Description { get; set; }
        public string? SigningReason { get; set; }
        public string? SigningLocation { get; set; }
        public string? StampImageUrl { get; set; }
        public string? WetSignatureImageUrl { get; set; }
        public bool IsShowDigitalSignature { get; set; }
        public bool IsDigitalSignatureInImage { get; set; }
        public bool AllowSigningSignedPdf { get; set; }
        public int StampIntensity { get; set; }
        public int WetSignatureIntensity { get; set; }
        public int WetSignatureStrokeWidth { get; set; }
        public string DigitalSignatureColor { get; set; } = String.Empty;
        public string? WetSignatureColor { get; set; }
        public string DigitalSignatureFont { get; set; } = String.Empty;
        public double DigitalSignatureFontSize { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public List<TemplatePositionDto> Positions { get; set; } = new();
    }
}
