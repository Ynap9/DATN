using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.Entities
{
    [Table(nameof(SubmissionLog), Schema = DbSchemas.Construction)]
    [Index(nameof(SubmissionId), nameof(CreatedDate), Name = "IX_SubmissionLog_SubmissionId_CreatedDate")]
    [Index(nameof(ProjectId), nameof(DocumentTypeId), nameof(CreatedDate), Name = "IX_SubmissionLog_ProjectId_DocumentTypeId_CreatedDate")]
    public class SubmissionLog : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int SubmissionId { get; private set; }
        public int ProjectId { get; private set; }
        public int DocumentTypeId { get; private set; }
        [MaxLength(450)]
        public string UserId { get; private set; } = String.Empty;
        public int OldStatus { get; private set; }
        public int NewStatus { get; private set; }
        [MaxLength(500)]
        public string Description { get; private set; } = String.Empty;
        [MaxLength(1000)]
        public string? Note { get; private set; }
        public int ChangedDataCount { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private SubmissionLog()
        {
        }

        public static SubmissionLog Create(int submissionId, int projectId, int documentTypeId, string userId, int oldStatus, int newStatus,
            string description, string? note, int changedDataCount, string? createdBy, DateTime createdDate)
        {
            return new SubmissionLog
            {
                SubmissionId = submissionId,
                ProjectId = projectId,
                DocumentTypeId = documentTypeId,
                UserId = userId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                Description = description,
                Note = note,
                ChangedDataCount = changedDataCount,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }
    }
}
