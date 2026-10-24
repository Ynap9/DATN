using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.Entities
{
    [Table(nameof(SubmissionQaLog), Schema = DbSchemas.Construction)]
    [Index(nameof(SubmissionId), nameof(CreatedDate), Name = "IX_SubmissionQaLog_SubmissionId_CreatedDate")]
    public class SubmissionQaLog : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int SubmissionId { get; private set; }
        public int ProjectId { get; private set; }
        public int DocumentTypeId { get; private set; }
        [MaxLength(450)]
        public string UserId { get; private set; } = String.Empty;
        public int OldStatusQa { get; private set; }
        public int NewStatusQa { get; private set; }
        public int OldStatusCheck { get; private set; }
        public int NewStatusCheck { get; private set; }
        [MaxLength(1000)]
        public string? Reason { get; private set; }
        [MaxLength(500)]
        public string Description { get; private set; } = String.Empty;
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private SubmissionQaLog()
        {
        }

        public static SubmissionQaLog Create(int submissionId, int projectId, int documentTypeId, string userId, int oldStatusQa, int newStatusQa,
            int oldStatusCheck, int newStatusCheck, string? reason, string description, string? createdBy, DateTime createdDate)
        {
            return new SubmissionQaLog
            {
                SubmissionId = submissionId,
                ProjectId = projectId,
                DocumentTypeId = documentTypeId,
                UserId = userId,
                OldStatusQa = oldStatusQa,
                NewStatusQa = newStatusQa,
                OldStatusCheck = oldStatusCheck,
                NewStatusCheck = newStatusCheck,
                Reason = reason,
                Description = description,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public List<SubmissionQaLogField> CreateWrongFields(IReadOnlyCollection<int> wrongDocumentFieldIds, string? createdBy, DateTime createdDate)
        {
            if (NewStatusQa != SubmissionConst.QA_FAILED)
            {
                return [];
            }
            return wrongDocumentFieldIds
                .Select(documentFieldId => SubmissionQaLogField.Create(Id, documentFieldId, createdBy, createdDate))
                .ToList();
        }
    }
}
