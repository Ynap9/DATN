using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Db;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.QaAssignments.Entities
{
    [Table(nameof(QaReviewerMode), Schema = DbSchemas.Construction)]
    [Index(nameof(ProjectId), nameof(UserId), IsUnique = true, Name = UniqueReviewerIndexName)]
    public class QaReviewerMode : ISoftDeleted
    {
        public const string UniqueReviewerIndexName = $"IX_{nameof(QaReviewerMode)}_{nameof(ProjectId)}_{nameof(UserId)}";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int ProjectId { get; private set; }
        [MaxLength(450)]
        public string UserId { get; private set; } = String.Empty;
        [MaxLength(QaAssignmentConst.MODE_MAX_LENGTH)]
        public string Mode { get; private set; } = QaAssignmentConst.MODE_FULL;
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private QaReviewerMode()
        {
        }

        public static QaReviewerMode Create(int projectId, string userId, string mode, string? createdBy, DateTime createdDate)
        {
            return new QaReviewerMode
            {
                ProjectId = projectId,
                UserId = userId,
                Mode = EnsureValidMode(mode),
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void ChangeMode(string mode, string? modifiedBy, DateTime modifiedDate)
        {
            Mode = EnsureValidMode(mode);
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        private static string EnsureValidMode(string mode)
        {
            if (!QaAssignmentConst.MODES.Contains(mode))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Chế độ chấm phải là {string.Join(" hoặc ", QaAssignmentConst.MODES)}");
            }
            return mode;
        }
    }
}
