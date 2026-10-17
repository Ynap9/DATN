using Microsoft.AspNetCore.Http;
using sh.be.shared.Constants.DigitalSignature;

namespace sh.be.applications.Module.DigitalSignature.Dtos.Template
{
    public class UpdateConfigTemplateDto
    {
        public string? SigningReason { get; set; }
        public string? SigningLocation { get; set; }
        public bool IsShowDigitalSignature { get; set; } = true;
        public bool IsDigitalSignatureInImage { get; set; }
        public bool AllowSigningSignedPdf { get; set; }
        public int StampIntensity { get; set; } = TemplateConst.DEFAULT_INTENSITY;
        public int WetSignatureIntensity { get; set; } = TemplateConst.DEFAULT_INTENSITY;
        public int WetSignatureStrokeWidth { get; set; } = TemplateConst.DEFAULT_STROKE_WIDTH;
        public string? DigitalSignatureColor { get; set; }
        public string? WetSignatureColor { get; set; }
        public string? DigitalSignatureFont { get; set; }
        public double DigitalSignatureFontSize { get; set; } = TemplateConst.DEFAULT_DIGITAL_SIGNATURE_FONT_SIZE;
        public IFormFile? StampImage { get; set; }
        public IFormFile? WetSignatureImage { get; set; }
        public bool IsRemoveStampImage { get; set; }
        public bool IsRemoveWetSignatureImage { get; set; }
        public List<TemplatePositionDto> Positions { get; set; } = new();
    }
}
