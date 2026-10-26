using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Ai;
using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Ai.Models.Entities
{
    [Table(nameof(AiModel), Schema = DbSchemas.Ai)]
    [Index(nameof(Code), IsUnique = true, Name = UniqueCodeIndexName)]
    [Index(nameof(ProviderId), Name = $"IX_{nameof(AiModel)}_{nameof(ProviderId)}")]
    public class AiModel : ISoftDeleted
    {
        public const string UniqueCodeIndexName = $"IX_{nameof(AiModel)}_{nameof(Code)}";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int ProviderId { get; private set; }
        [MaxLength(AiModelConst.CODE_MAX_LENGTH)]
        public string Code { get; private set; } = String.Empty;
        [MaxLength(AiModelConst.DISPLAY_NAME_MAX_LENGTH)]
        public string DisplayName { get; private set; } = String.Empty;
        [MaxLength(AiModelConst.REMOTE_MODEL_NAME_MAX_LENGTH)]
        public string RemoteModelName { get; private set; } = String.Empty;
        [MaxLength(AiModelConst.CAPABILITIES_MAX_LENGTH)]
        public string Capabilities { get; private set; } = String.Empty;
        public int MaxOutputTokens { get; private set; }
        [MaxLength(AiModelConst.DEFAULT_PARAMS_JSON_MAX_LENGTH)]
        public string DefaultParamsJson { get; private set; } = String.Empty;
        public string? RequestTemplateJson { get; private set; }
        [MaxLength(AiModelConst.RESPONSE_MAPPING_JSON_MAX_LENGTH)]
        public string? ResponseMappingJson { get; private set; }
        public bool IsActive { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private AiModel()
        {
        }

        public static AiModel Create(int providerId, string code, string displayName, string remoteModelName, string capabilities, int maxOutputTokens,
            string defaultParamsJson, string? requestTemplateJson, string? responseMappingJson, bool isActive, string? createdBy, DateTime createdDate)
        {
            return new AiModel
            {
                ProviderId = providerId,
                Code = code,
                DisplayName = displayName,
                RemoteModelName = remoteModelName,
                Capabilities = capabilities,
                MaxOutputTokens = maxOutputTokens,
                DefaultParamsJson = defaultParamsJson,
                RequestTemplateJson = requestTemplateJson,
                ResponseMappingJson = responseMappingJson,
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
