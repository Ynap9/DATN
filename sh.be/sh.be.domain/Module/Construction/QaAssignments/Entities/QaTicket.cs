using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Construction.QaAssignments.Models;
using sh.be.domain.Module.Construction.Submissions.Entities;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Db;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.QaAssignments.Entities
{
    [Table(nameof(QaTicket), Schema = DbSchemas.Construction)]
    [Index(nameof(ProjectId), nameof(DocumentTypeId), nameof(QaUserId), Name = $"IX_{nameof(QaTicket)}_{nameof(ProjectId)}_{nameof(DocumentTypeId)}_{nameof(QaUserId)}")]
    [Index(nameof(Status), Name = $"IX_{nameof(QaTicket)}_{nameof(Status)}")]
    public class QaTicket : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int ProjectId { get; private set; }
        public int DocumentTypeId { get; private set; }
        [MaxLength(450)]
        public string QaUserId { get; private set; } = String.Empty;
        [MaxLength(QaAssignmentConst.MODE_MAX_LENGTH)]
        public string Mode { get; private set; } = QaAssignmentConst.MODE_FULL;
        public int DocumentCount { get; private set; }
        public int SampleSize { get; private set; }
        public int MaxFailed { get; private set; }
        [Precision(5, 2)]
        public decimal ThresholdPercent { get; private set; }
        [MaxLength(QaAssignmentConst.STRATEGY_CODE_MAX_LENGTH)]
        public string StrategyCode { get; private set; } = String.Empty;
        [MaxLength(QaAssignmentConst.STRATEGY_PARAMS_MAX_LENGTH)]
        public string? StrategyParamsJson { get; private set; }
        public int Seed { get; private set; }
        public bool StopEarlyOnReject { get; private set; }
        public int Round { get; private set; }
        public DateTime RoundStartedDate { get; private set; }
        public int RejectedCount { get; private set; }
        [MaxLength(QaAssignmentConst.TICKET_STATUS_MAX_LENGTH)]
        public string Status { get; private set; } = QaAssignmentConst.TICKET_STATUS_ACTIVE;
        public int ReviewedCount { get; private set; }
        public int FailedCount { get; private set; }
        public DateTime? ConcludedDate { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private QaTicket()
        {
        }

        public static QaTicket Create(int projectId, int documentTypeId, string qaUserId, string mode, int documentCount, int sampleSize, int maxFailed,
            decimal thresholdPercent, string strategyCode, string? strategyParamsJson, int seed, bool stopEarlyOnReject, string? createdBy,
            DateTime createdDate)
        {
            return new QaTicket
            {
                ProjectId = projectId,
                DocumentTypeId = documentTypeId,
                QaUserId = qaUserId,
                Mode = mode,
                DocumentCount = documentCount,
                SampleSize = sampleSize,
                MaxFailed = maxFailed,
                ThresholdPercent = thresholdPercent,
                StrategyCode = strategyCode,
                StrategyParamsJson = strategyParamsJson,
                Seed = seed,
                StopEarlyOnReject = stopEarlyOnReject,
                Round = 1,
                RoundStartedDate = createdDate,
                Status = QaAssignmentConst.TICKET_STATUS_ACTIVE,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public static QaTicket CreateFull(int projectId, int documentTypeId, string qaUserId, int documentCount, string? createdBy, DateTime createdDate)
        {
            return new QaTicket
            {
                ProjectId = projectId,
                DocumentTypeId = documentTypeId,
                QaUserId = qaUserId,
                Mode = QaAssignmentConst.MODE_FULL,
                DocumentCount = documentCount,
                Round = 1,
                RoundStartedDate = createdDate,
                Status = QaAssignmentConst.TICKET_STATUS_ACTIVE,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public List<SubmissionUser> AssignSubmissions(IEnumerable<QaCandidate> candidates, string? createdBy, DateTime createdDate)
        {
            return candidates
                .Select(candidate => SubmissionUser.Create(candidate.SubmissionId, ProjectId, DocumentTypeId, RoleConstants.ROLE_QA, QaUserId, Id,
                    createdBy, createdDate))
                .ToList();
        }

        public bool IsAssignedTo(string userId)
        {
            return QaUserId == userId;
        }

        public bool IsOpen()
        {
            return Status is QaAssignmentConst.TICKET_STATUS_ACTIVE or QaAssignmentConst.TICKET_STATUS_REJECTED;
        }

        public void Pass(string? modifiedBy, DateTime concludedDate)
        {
            if (Status != QaAssignmentConst.TICKET_STATUS_ACTIVE)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorQaTicketStatusInvalid);
            }
            Status = QaAssignmentConst.TICKET_STATUS_PASSED;
            ConcludedDate = concludedDate;
            ModifiedBy = modifiedBy;
            ModifiedDate = concludedDate;
        }

        public void Cancel(string? modifiedBy, DateTime modifiedDate)
        {
            if (!IsOpen())
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorQaTicketStatusInvalid);
            }
            Status = QaAssignmentConst.TICKET_STATUS_CANCELLED;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }
    }
}
