using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Packaging.Entities
{
    [Table(nameof(PackageSchemaField), Schema = DbSchemas.Packaging)]
    [Index(nameof(PackageSchemaId), nameof(ElementName), IsUnique = true, Name = UniqueElementIndexName)]
    [Index(nameof(PackageSchemaId), nameof(SortOrder), Name = $"IX_{nameof(PackageSchemaField)}_{nameof(PackageSchemaId)}_{nameof(SortOrder)}")]
    public class PackageSchemaField : ISoftDeleted
    {
        public const string UniqueElementIndexName = $"IX_{nameof(PackageSchemaField)}_{nameof(PackageSchemaId)}_{nameof(ElementName)}";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int PackageSchemaId { get; private set; }
        [MaxLength(50)]
        public string ElementName { get; private set; } = String.Empty;
        [MaxLength(200)]
        public string DisplayName { get; private set; } = String.Empty;
        public int DataType { get; private set; }
        public int? MaxLength { get; private set; }
        public int Requirement { get; private set; }
        [MaxLength(50)]
        public string? ConditionElement { get; private set; }
        [MaxLength(100)]
        public string? ConditionValues { get; private set; }
        [MaxLength(50)]
        public string? CodeGroup { get; private set; }
        public bool AllowMultipleValues { get; private set; }
        [MaxLength(2000)]
        public string? Description { get; private set; }
        public int SortOrder { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private PackageSchemaField()
        {
        }

        public static PackageSchemaField Create(int packageSchemaId, string elementName, string displayName, int dataType, int? maxLength,
            int requirement, string? conditionElement, string? conditionValues, string? codeGroup, bool allowMultipleValues,
            string? description, int sortOrder, string? createdBy, DateTime? createdDate)
        {
            return new PackageSchemaField
            {
                PackageSchemaId = packageSchemaId,
                ElementName = elementName,
                DisplayName = displayName,
                DataType = dataType,
                MaxLength = maxLength,
                Requirement = requirement,
                ConditionElement = conditionElement,
                ConditionValues = conditionValues,
                CodeGroup = codeGroup,
                AllowMultipleValues = allowMultipleValues,
                Description = description,
                SortOrder = sortOrder,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }
    }
}
