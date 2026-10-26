using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Ai;
using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Ai.Prompts.Entities
{
    [Table(nameof(PromptFieldConfig), Schema = DbSchemas.Ai)]
    [Index(nameof(PromptTemplateId), nameof(DocumentFieldId), IsUnique = true, Name = UniqueFieldIndexName)]
    [Index(nameof(PromptTemplateId), nameof(OutputKey), IsUnique = true, Name = UniqueOutputKeyIndexName)]
    public class PromptFieldConfig : ISoftDeleted
    {
        public const string UniqueFieldIndexName = $"IX_{nameof(PromptFieldConfig)}_{nameof(PromptTemplateId)}_{nameof(DocumentFieldId)}";
        public const string UniqueOutputKeyIndexName = $"IX_{nameof(PromptFieldConfig)}_{nameof(PromptTemplateId)}_{nameof(OutputKey)}";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int PromptTemplateId { get; private set; }
        public int DocumentFieldId { get; private set; }
        [MaxLength(PromptTemplateConst.OUTPUT_KEY_MAX_LENGTH)]
        public string OutputKey { get; private set; } = String.Empty;
        [MaxLength(PromptTemplateConst.FIELD_DESCRIPTION_MAX_LENGTH)]
        public string Description { get; private set; } = String.Empty;
        [MaxLength(PromptTemplateConst.FIELD_EXAMPLE_MAX_LENGTH)]
        public string? Example { get; private set; }
        [MaxLength(PromptTemplateConst.NORMALIZE_RULE_MAX_LENGTH)]
        public string NormalizeRule { get; private set; } = String.Empty;
        public bool IsRequired { get; private set; }
        public int Order { get; private set; }
        public bool IsActive { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private PromptFieldConfig()
        {
        }

        public static PromptFieldConfig Create(int promptTemplateId, int documentFieldId, string outputKey, string description, string? example,
            string normalizeRule, bool isRequired, int order, bool isActive, string? createdBy, DateTime createdDate)
        {
            return new PromptFieldConfig
            {
                PromptTemplateId = promptTemplateId,
                DocumentFieldId = documentFieldId,
                OutputKey = outputKey,
                Description = description,
                Example = example,
                NormalizeRule = normalizeRule,
                IsRequired = isRequired,
                Order = order,
                IsActive = isActive,
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
    }
}
