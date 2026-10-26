using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Db;
using sh.be.shared.Constants.Operation;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.Submissions.Entities
{
    [Table(nameof(SubmissionUser), Schema = DbSchemas.Construction)]
    [Index(nameof(SubmissionId), nameof(Role), IsUnique = true, Name = UniqueRoleIndexName)]
    [Index(nameof(TicketId), nameof(Role), Name = $"IX_{nameof(SubmissionUser)}_{nameof(TicketId)}_{nameof(Role)}")]
    [Index(nameof(ProjectId), nameof(DocumentTypeId), nameof(Role), nameof(UserId),
        Name = $"IX_{nameof(SubmissionUser)}_{nameof(ProjectId)}_{nameof(DocumentTypeId)}_{nameof(Role)}_{nameof(UserId)}")]
    [Index(nameof(UserId), Name = $"IX_{nameof(SubmissionUser)}_{nameof(UserId)}")]
    public class SubmissionUser
    {
        public const string UniqueRoleIndexName = $"IX_{nameof(SubmissionUser)}_{nameof(SubmissionId)}_{nameof(Role)}";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int SubmissionId { get; private set; }
        public int ProjectId { get; private set; }
        public int DocumentTypeId { get; private set; }
        [MaxLength(QaAssignmentConst.SUBMISSION_USER_ROLE_MAX_LENGTH)]
        public string Role { get; private set; } = String.Empty;
        [MaxLength(450)]
        public string UserId { get; private set; } = String.Empty;
        public int TicketId { get; private set; }
        public bool IsSample { get; private set; }
        public int ReturnCount { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }

        private SubmissionUser()
        {
        }

        public static SubmissionUser Create(int submissionId, int projectId, int documentTypeId, string role, string userId, int ticketId,
            string? createdBy, DateTime createdDate)
        {
            if (!QaAssignmentConst.SUBMISSION_USER_ROLES.Contains(role))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Vai trò được giao phải là {string.Join(" hoặc ", QaAssignmentConst.SUBMISSION_USER_ROLES)}");
            }
            return new SubmissionUser
            {
                SubmissionId = submissionId,
                ProjectId = projectId,
                DocumentTypeId = documentTypeId,
                Role = role,
                UserId = userId,
                TicketId = ticketId,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public static SubmissionUser CreateEntry(int submissionId, int projectId, int documentTypeId, string userId, int ticketId,
            string? createdBy, DateTime createdDate)
        {
            return Create(submissionId, projectId, documentTypeId, RoleConstants.ROLE_DATA, userId, ticketId, createdBy, createdDate);
        }

        public bool IncreaseReturnAndShouldRelease(string? modifiedBy, DateTime modifiedDate)
        {
            if (Role != RoleConstants.ROLE_DATA)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Chỉ đếm lần bị trả cho vai trò {RoleConstants.ROLE_DATA}");
            }
            ReturnCount++;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
            return ReturnCount > AssignmentConst.ENTRY_RETURNS_BEFORE_POOL;
        }

        public void ChangeTicket(int ticketId, string? modifiedBy, DateTime modifiedDate)
        {
            TicketId = ticketId;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }
    }
}
