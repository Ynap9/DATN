using sh.be.shared.Constants.Db;
using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace sh.be.domain.Module.DigitalSignature.Entities
{
    [Table(nameof(Template), Schema = DbSchemas.DigitalSignature)]
    public class Template : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        [MaxLength(250)]
        public string Name { get; private set; } = String.Empty;
        public string? Description { get; private set; }
        [MaxLength(500)]
        public string? SigningReason { get; private set; }
        [MaxLength(500)]
        public string? SigningLocation { get; private set; }
        [MaxLength(1000)]
        public string? StampImageUrl { get; private set; }
        [MaxLength(1000)]
        public string? StampImageObjectKey { get; private set; }
        [MaxLength(1000)]
        public string? WetSignatureImageUrl { get; private set; }
        [MaxLength(1000)]
        public string? WetSignatureImageObjectKey { get; private set; }
        public bool IsShowDigitalSignature { get; private set; } = true;
        public bool IsDigitalSignatureInImage { get; private set; }
        public bool AllowSigningSignedPdf { get; private set; }
        public int StampIntensity { get; private set; } = TemplateConst.DEFAULT_INTENSITY;
        public int WetSignatureIntensity { get; private set; } = TemplateConst.DEFAULT_INTENSITY;
        public int WetSignatureStrokeWidth { get; private set; } = TemplateConst.DEFAULT_STROKE_WIDTH;
        [MaxLength(7)]
        public string DigitalSignatureColor { get; private set; } = TemplateConst.DEFAULT_DIGITAL_SIGNATURE_COLOR;
        [MaxLength(100)]
        public string DigitalSignatureFont { get; private set; } = TemplateConst.DEFAULT_DIGITAL_SIGNATURE_FONT;
        public double DigitalSignatureFontSize { get; private set; } = TemplateConst.DEFAULT_DIGITAL_SIGNATURE_FONT_SIZE;
        [MaxLength(7)]
        public string? WetSignatureColor { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private Template()
        {
        }

        public static Template Create(string name, string? description, string? createdBy, DateTime? createdDate)
        {
            return new Template
            {
                Name = name.Trim(),
                Description = description?.Trim(),
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void Update(string name, string? modifiedBy, DateTime modifiedDate)
        {
            Name = name.Trim();
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void UpdateConfig(string? signingReason, string? signingLocation, bool isShowDigitalSignature, bool isDigitalSignatureInImage,
            bool allowSigningSignedPdf, int stampIntensity, int wetSignatureIntensity, int wetSignatureStrokeWidth,
            string? digitalSignatureColor, string? wetSignatureColor, string? digitalSignatureFont, double digitalSignatureFontSize,
            string? modifiedBy, DateTime modifiedDate)
        {
            SigningReason = signingReason;
            SigningLocation = signingLocation;
            IsShowDigitalSignature = isShowDigitalSignature;
            IsDigitalSignatureInImage = isDigitalSignatureInImage;
            AllowSigningSignedPdf = allowSigningSignedPdf;
            StampIntensity = Math.Clamp(stampIntensity, TemplateConst.MIN_INTENSITY, TemplateConst.MAX_INTENSITY);
            WetSignatureIntensity = Math.Clamp(wetSignatureIntensity, TemplateConst.MIN_INTENSITY, TemplateConst.MAX_INTENSITY);
            WetSignatureStrokeWidth = Math.Clamp(wetSignatureStrokeWidth, TemplateConst.MIN_STROKE_WIDTH, TemplateConst.MAX_STROKE_WIDTH);
            DigitalSignatureColor = NormalizeColor(digitalSignatureColor) ?? TemplateConst.DEFAULT_DIGITAL_SIGNATURE_COLOR;
            WetSignatureColor = NormalizeColor(wetSignatureColor);
            DigitalSignatureFont = TemplateConst.DIGITAL_SIGNATURE_FONTS
                .FirstOrDefault(font => string.Equals(font, digitalSignatureFont?.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? TemplateConst.DEFAULT_DIGITAL_SIGNATURE_FONT;
            DigitalSignatureFontSize = Math.Clamp(digitalSignatureFontSize,
                TemplateConst.MIN_DIGITAL_SIGNATURE_FONT_SIZE, TemplateConst.MAX_DIGITAL_SIGNATURE_FONT_SIZE);
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void ChangeStampImage(string url, string objectKey)
        {
            StampImageUrl = url;
            StampImageObjectKey = objectKey;
        }

        public void RemoveStampImage()
        {
            StampImageUrl = null;
            StampImageObjectKey = null;
        }

        public void ChangeWetSignatureImage(string url, string objectKey)
        {
            WetSignatureImageUrl = url;
            WetSignatureImageObjectKey = objectKey;
        }

        public void RemoveWetSignatureImage()
        {
            WetSignatureImageUrl = null;
            WetSignatureImageObjectKey = null;
        }

        public void Delete(string? deletedBy, DateTime deletedDate)
        {
            Deleted = true;
            DeletedDate = deletedDate;
            DeletedBy = deletedBy;
            RemoveStampImage();
            RemoveWetSignatureImage();
        }

        public TemplatePosition CreatePosition(int kind, int pageNumber, double xRatio, double yRatio, double widthRatio, double heightRatio,
            string? createdBy, DateTime? createdDate)
        {
            return TemplatePosition.Create(Id, kind, pageNumber, xRatio, yRatio, widthRatio, heightRatio, createdBy, createdDate);
        }

        private static string? NormalizeColor(string? color)
        {
            var value = color?.Trim() ?? String.Empty;
            if (value.Length == 0)
            {
                return null;
            }
            if (!value.StartsWith('#'))
            {
                value = "#" + value;
            }
            return Regex.IsMatch(value, TemplateConst.COLOR_PATTERN) ? value.ToUpperInvariant() : null;
        }
    }
}
