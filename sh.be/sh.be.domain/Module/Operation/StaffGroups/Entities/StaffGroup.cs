using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using sh.be.shared.Constants.Operation;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Operation.StaffGroups.Entities
{
    [Table(nameof(StaffGroup), Schema = DbSchemas.Operation)]
    [Index(nameof(OwnerUserId), nameof(Name), IsUnique = true, Name = UniqueOwnerNameIndexName)]
    public class StaffGroup : ISoftDeleted
    {
        public const string UniqueOwnerNameIndexName = $"IX_{nameof(StaffGroup)}_{nameof(OwnerUserId)}_{nameof(Name)}";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        [MaxLength(StaffGroupConst.NAME_MAX_LENGTH)]
        public string Name { get; private set; } = String.Empty;
        [MaxLength(StaffGroupConst.DESCRIPTION_MAX_LENGTH)]
        public string? Description { get; private set; }
        [MaxLength(450)]
        public string OwnerUserId { get; private set; } = String.Empty;
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private StaffGroup()
        {
        }

        public static StaffGroup Create(string name, string? description, string ownerUserId, string? createdBy, DateTime createdDate)
        {
            var trimmedDescription = description?.Trim();
            return new StaffGroup
            {
                Name = name.Trim(),
                Description = String.IsNullOrEmpty(trimmedDescription) ? null : trimmedDescription,
                OwnerUserId = ownerUserId,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void Update(string name, string? description, string? modifiedBy, DateTime modifiedDate)
        {
            var trimmedDescription = description?.Trim();
            Name = name.Trim();
            Description = String.IsNullOrEmpty(trimmedDescription) ? null : trimmedDescription;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Delete(string? deletedBy, DateTime deletedDate)
        {
            Deleted = true;
            DeletedDate = deletedDate;
            DeletedBy = deletedBy;
        }

        public bool IsOwnedBy(string userId)
        {
            return OwnerUserId == userId;
        }
    }
}
