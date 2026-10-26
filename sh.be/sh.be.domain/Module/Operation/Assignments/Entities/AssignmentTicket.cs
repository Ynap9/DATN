using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Db;
using sh.be.shared.Constants.Operation;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Operation.Assignments.Entities
{
    [Table(nameof(AssignmentTicket), Schema = DbSchemas.Operation)]
    [Index(nameof(ProjectId), nameof(DocumentTypeId), nameof(AssigneeUserId), nameof(Status),
        Name = $"IX_{nameof(AssignmentTicket)}_{nameof(ProjectId)}_{nameof(DocumentTypeId)}_{nameof(AssigneeUserId)}_{nameof(Status)}")]
    [Index(nameof(Status), nameof(DoneAt), Name = $"IX_{nameof(AssignmentTicket)}_{nameof(Status)}_{nameof(DoneAt)}")]
    public class AssignmentTicket
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int ProjectId { get; private set; }
        public int DocumentTypeId { get; private set; }
        [MaxLength(450)]
        public string AssigneeUserId { get; private set; } = String.Empty;
        [MaxLength(ProjectMemberConst.ROLE_MAX_LENGTH)]
        public string Role { get; private set; } = RoleConstants.ROLE_DATA;
        public int Mode { get; private set; } = AssignmentConst.MODE_MANUAL;
        public int Status { get; private set; } = AssignmentConst.STATUS_ACTIVE;
        public int DocumentCount { get; private set; }
        public int? SourceTicketId { get; private set; }
        public DateTime? DoneAt { get; private set; }
        public DateTime? CancelledAt { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }

        private AssignmentTicket()
        {
        }

        public static AssignmentTicket CreateManual(int projectId, int documentTypeId, string assigneeUserId, string role, int documentCount,
            int? sourceTicketId, string? createdBy, DateTime createdDate)
        {
            if (role != RoleConstants.ROLE_DATA)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Phiếu nhập liệu chỉ giao vai trò {RoleConstants.ROLE_DATA}");
            }
            return new AssignmentTicket
            {
                ProjectId = projectId,
                DocumentTypeId = documentTypeId,
                AssigneeUserId = assigneeUserId,
                Role = role,
                Mode = AssignmentConst.MODE_MANUAL,
                Status = AssignmentConst.STATUS_ACTIVE,
                DocumentCount = documentCount,
                SourceTicketId = sourceTicketId,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void DecreaseDocuments(int count, string? modifiedBy, DateTime modifiedDate)
        {
            EnsureActive();
            DocumentCount = Math.Max(0, DocumentCount - count);
            if (DocumentCount == 0)
            {
                Status = AssignmentConst.STATUS_CANCELLED;
                CancelledAt = modifiedDate;
            }
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void MarkDone(DateTime doneAt)
        {
            EnsureActive();
            Status = AssignmentConst.STATUS_DONE;
            DoneAt = doneAt;
            ModifiedDate = doneAt;
        }

        public void EnsureActive()
        {
            if (Status != AssignmentConst.STATUS_ACTIVE)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorTicketNotActive);
            }
        }
    }
}
