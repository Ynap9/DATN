using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Packaging.Entities
{
    [Table(nameof(PackageCodeValue), Schema = DbSchemas.Packaging)]
    [Index(nameof(CodeGroup), nameof(Code), IsUnique = true, Name = UniqueCodeIndexName)]
    public class PackageCodeValue : ISoftDeleted
    {
        public const string UniqueCodeIndexName = $"IX_{nameof(PackageCodeValue)}_{nameof(CodeGroup)}_{nameof(Code)}";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        [MaxLength(50)]
        public string CodeGroup { get; private set; } = String.Empty;
        [MaxLength(10)]
        public string Code { get; private set; } = String.Empty;
        [MaxLength(200)]
        public string Name { get; private set; } = String.Empty;
        public int SortOrder { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private PackageCodeValue()
        {
        }

        public static PackageCodeValue Create(string codeGroup, string code, string name, int sortOrder,
            string? createdBy, DateTime? createdDate)
        {
            return new PackageCodeValue
            {
                CodeGroup = codeGroup,
                Code = code,
                Name = name,
                SortOrder = sortOrder,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }
    }
}
