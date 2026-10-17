using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.DigitalSignature.Entities
{
    [Table(nameof(TemplatePosition), Schema = DbSchemas.DigitalSignature)]
    [Index(nameof(TemplateId), Name = $"IX_{nameof(TemplatePosition)}_{nameof(TemplateId)}")]
    public class TemplatePosition : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int TemplateId { get; private set; }
        public int Kind { get; private set; }
        public int PageNumber { get; private set; } = 1;
        public double XRatio { get; private set; }
        public double YRatio { get; private set; }
        public double WidthRatio { get; private set; }
        public double HeightRatio { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private TemplatePosition()
        {
        }

        public static TemplatePosition Create(int templateId, int kind, int pageNumber, double xRatio, double yRatio, double widthRatio, double heightRatio,
            string? createdBy, DateTime? createdDate)
        {
            EnsureValidKind(kind);
            EnsureInsidePage(pageNumber, xRatio, yRatio, widthRatio, heightRatio);

            return new TemplatePosition
            {
                TemplateId = templateId,
                Kind = kind,
                PageNumber = pageNumber,
                XRatio = xRatio,
                YRatio = yRatio,
                WidthRatio = widthRatio,
                HeightRatio = heightRatio,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void Delete(string? deletedBy, DateTime deletedDate)
        {
            Deleted = true;
            DeletedDate = deletedDate;
            DeletedBy = deletedBy;
        }

        private static void EnsureValidKind(int kind)
        {
            if (!TemplatePositionConst.KINDS.Contains(kind))
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorInvalidTemplatePositionKind);
            }
        }

        private static void EnsureInsidePage(int pageNumber, double xRatio, double yRatio, double widthRatio, double heightRatio)
        {
            if (pageNumber < 1
                || widthRatio <= 0 || heightRatio <= 0
                || xRatio < 0 || yRatio < 0
                || xRatio + widthRatio > 1
                || yRatio + heightRatio > 1)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorTemplatePositionOutOfPage);
            }
        }
    }
}
