using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Operation.ProjectMembers.Services;
using sh.be.shared.Constants.Db;
using sh.be.shared.Constants.Operation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Operation.ProjectMembers.Entities
{
    [Table(nameof(ProjectPriceDefault), Schema = DbSchemas.Operation)]
    [Index(nameof(ProjectId), nameof(DocumentTypeId), nameof(Role), nameof(EffectiveFrom),
        Name = $"IX_{nameof(ProjectPriceDefault)}_{nameof(ProjectId)}_{nameof(DocumentTypeId)}_{nameof(Role)}_{nameof(EffectiveFrom)}")]
    public class ProjectPriceDefault
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int ProjectId { get; private set; }
        public int DocumentTypeId { get; private set; }
        [MaxLength(ProjectMemberConst.ROLE_MAX_LENGTH)]
        public string Role { get; private set; } = String.Empty;
        [Precision(PriceConst.PRICE_PRECISION, PriceConst.PRICE_SCALE)]
        public decimal CompanyPrice { get; private set; }
        [Precision(PriceConst.PRICE_PRECISION, PriceConst.PRICE_SCALE)]
        public decimal ActualPrice { get; private set; }
        public DateTime EffectiveFrom { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }

        private ProjectPriceDefault()
        {
        }

        public static ProjectPriceDefault Create(int projectId, int documentTypeId, string role, decimal companyPrice, decimal actualPrice,
            DateTime effectiveFrom, string? createdBy, DateTime createdDate)
        {
            PricePolicy.EnsureValid(companyPrice, actualPrice);
            return new ProjectPriceDefault
            {
                ProjectId = projectId,
                DocumentTypeId = documentTypeId,
                Role = role,
                CompanyPrice = companyPrice,
                ActualPrice = actualPrice,
                EffectiveFrom = effectiveFrom.Date,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }
    }
}
