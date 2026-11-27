using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Ai;
using sh.be.shared.Constants.Db;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Ai.Prompts.Entities
{
    [Table(nameof(PromptTemplate), Schema = DbSchemas.Ai)]
    [Index(nameof(DocumentTypeId), nameof(Version), IsUnique = true, Name = UniqueVersionIndexName)]
    [Index(nameof(DocumentTypeId), IsUnique = true, Name = UniquePublishedIndexName)]
    public class PromptTemplate : ISoftDeleted
    {
        public const string UniqueVersionIndexName = $"IX_{nameof(PromptTemplate)}_{nameof(DocumentTypeId)}_{nameof(Version)}";
        public const string UniquePublishedIndexName = $"IX_{nameof(PromptTemplate)}_{nameof(DocumentTypeId)}_Published";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int DocumentTypeId { get; private set; }
        public int Version { get; private set; }
        public string SystemPrompt { get; private set; } = String.Empty;
        public string InstructionPrompt { get; private set; } = String.Empty;
        [MaxLength(PromptTemplateConst.STATUS_MAX_LENGTH)]
        public string Status { get; private set; } = String.Empty;
        public int? SourceTemplateId { get; private set; }
        [MaxLength(PromptTemplateConst.NOTE_MAX_LENGTH)]
        public string? Note { get; private set; }
        public string? PublishedBy { get; private set; }
        public DateTime? PublishedDate { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        // Ngày thay đổi cuối API trả cho màn sửa: nháp mới khôi phục chưa sửa lần nào có ModifiedDate trống nên lấy ngày tạo
        public DateTime? LastChangedDate => ModifiedDate ?? CreatedDate;

        private PromptTemplate()
        {
        }

        public static PromptTemplate Create(int documentTypeId, int version, string systemPrompt, string instructionPrompt, int? sourceTemplateId,
            string? note, string? createdBy, DateTime createdDate)
        {
            return new PromptTemplate
            {
                DocumentTypeId = documentTypeId,
                Version = version,
                SystemPrompt = systemPrompt,
                InstructionPrompt = instructionPrompt,
                Status = PromptTemplateConst.STATUS_DRAFT,
                SourceTemplateId = sourceTemplateId,
                Note = note,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void EnsureDraft()
        {
            if (Status != PromptTemplateConst.STATUS_DRAFT)
            {
                throw new UserFriendlyException(ErrorCodes.AiErrorPromptTemplateNotDraft);
            }
        }

        public void UpdateDraft(string systemPrompt, string instructionPrompt, string? note, string? modifiedBy, DateTime modifiedDate)
        {
            EnsureDraft();
            var trimmedSystemPrompt = systemPrompt.Trim();
            var trimmedInstructionPrompt = instructionPrompt.Trim();
            if (trimmedSystemPrompt.Length > PromptTemplateConst.PROMPT_MAX_LENGTH || trimmedInstructionPrompt.Length > PromptTemplateConst.PROMPT_MAX_LENGTH)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Prompt chung và hướng dẫn trích xuất tối đa {PromptTemplateConst.PROMPT_MAX_LENGTH} ký tự");
            }

            SystemPrompt = trimmedSystemPrompt;
            InstructionPrompt = trimmedInstructionPrompt;
            Note = NormalizeNote(note);
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Publish(string? note, string? publishedBy, DateTime publishedDate)
        {
            EnsureDraft();
            Note = NormalizeNote(note) ?? Note;
            Status = PromptTemplateConst.STATUS_PUBLISHED;
            PublishedBy = publishedBy;
            PublishedDate = publishedDate;
            ModifiedBy = publishedBy;
            ModifiedDate = publishedDate;
        }

        public void Archive(string? modifiedBy, DateTime modifiedDate)
        {
            Status = PromptTemplateConst.STATUS_ARCHIVED;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Delete(string? deletedBy, DateTime deletedDate)
        {
            Deleted = true;
            DeletedDate = deletedDate;
            DeletedBy = deletedBy;
        }

        private static string? NormalizeNote(string? note)
        {
            var trimmedNote = note?.Trim();
            if (trimmedNote?.Length > PromptTemplateConst.NOTE_MAX_LENGTH)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Ghi chú phiên bản tối đa {PromptTemplateConst.NOTE_MAX_LENGTH} ký tự");
            }
            return String.IsNullOrEmpty(trimmedNote) ? null : trimmedNote;
        }
    }
}
