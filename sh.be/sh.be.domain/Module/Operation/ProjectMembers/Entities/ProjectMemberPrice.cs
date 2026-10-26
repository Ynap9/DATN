using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Operation.ProjectMembers.Services;
using sh.be.shared.Constants.Db;
using sh.be.shared.Constants.Operation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Operation.ProjectMembers.Entities
{
    [Table(nameof(ProjectMemberPrice), Schema = DbSchemas.Operation)]
    [Index(nameof(ProjectMemberId), nameof(EffectiveFrom), Name = $"IX_{nameof(ProjectMemberPrice)}_{nameof(ProjectMemberId)}_{nameof(EffectiveFrom)}")]
    public class ProjectMemberPrice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int ProjectMemberId { get; private set; }
        [Precision(PriceConst.PRICE_PRECISION, PriceConst.PRICE_SCALE)]
        public decimal CompanyPrice { get; private set; }
        [Precision(PriceConst.PRICE_PRECISION, PriceConst.PRICE_SCALE)]
        public decimal ActualPrice { get; private set; }
        public DateTime EffectiveFrom { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }

        private ProjectMemberPrice()
        {
        }

        public static ProjectMemberPrice Create(int projectMemberId, decimal companyPrice, decimal actualPrice, DateTime effectiveFrom,
            string? createdBy, DateTime createdDate)
        {
            PricePolicy.EnsureValid(companyPrice, actualPrice);
            return new ProjectMemberPrice
            {
                ProjectMemberId = projectMemberId,
                CompanyPrice = companyPrice,
                ActualPrice = actualPrice,
                EffectiveFrom = effectiveFrom.Date,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }
    }
}
