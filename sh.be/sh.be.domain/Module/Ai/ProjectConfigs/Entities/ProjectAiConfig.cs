using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Ai;
using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Ai.ProjectConfigs.Entities
{
    [Table(nameof(ProjectAiConfig), Schema = DbSchemas.Ai)]
    [Index(nameof(ProjectId), nameof(DocumentTypeId), IsUnique = true, Name = UniqueProjectDocumentTypeIndexName)]
    [Index(nameof(PrimaryModelId), Name = $"IX_{nameof(ProjectAiConfig)}_{nameof(PrimaryModelId)}")]
    [Index(nameof(FallbackModelId), Name = $"IX_{nameof(ProjectAiConfig)}_{nameof(FallbackModelId)}")]
    public class ProjectAiConfig : ISoftDeleted
    {
        public const string UniqueProjectDocumentTypeIndexName = $"IX_{nameof(ProjectAiConfig)}_{nameof(ProjectId)}_{nameof(DocumentTypeId)}";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int ProjectId { get; private set; }
        public int DocumentTypeId { get; private set; }
        [MaxLength(ProjectAiConfigConst.AI_MODE_MAX_LENGTH)]
        public string AiMode { get; private set; } = String.Empty;
        public int PrimaryModelId { get; private set; }
        public int? FallbackModelId { get; private set; }
        public int? PromptTemplateId { get; private set; }
        [MaxLength(ProjectAiConfigConst.PARAMS_OVERRIDE_JSON_MAX_LENGTH)]
        public string? ParamsOverrideJson { get; private set; }
        [MaxLength(ProjectAiConfigConst.EXTRA_INSTRUCTION_MAX_LENGTH)]
        public string? ExtraInstruction { get; private set; }
        [MaxLength(ProjectAiConfigConst.PAGE_RULE_JSON_MAX_LENGTH)]
        public string? PageRuleJson { get; private set; }
        public int ParallelLimit { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private ProjectAiConfig()
        {
        }

        public static ProjectAiConfig Create(int projectId, int documentTypeId, string aiMode, int primaryModelId, int? fallbackModelId, int? promptTemplateId,
            string? paramsOverrideJson, string? extraInstruction, string? pageRuleJson, int parallelLimit, string? createdBy, DateTime createdDate)
        {
            return new ProjectAiConfig
            {
                ProjectId = projectId,
                DocumentTypeId = documentTypeId,
                AiMode = aiMode,
                PrimaryModelId = primaryModelId,
                FallbackModelId = fallbackModelId,
                PromptTemplateId = promptTemplateId,
                ParamsOverrideJson = paramsOverrideJson,
                ExtraInstruction = extraInstruction,
                PageRuleJson = pageRuleJson,
                ParallelLimit = parallelLimit,
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
