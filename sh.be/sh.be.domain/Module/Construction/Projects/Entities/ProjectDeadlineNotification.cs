using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.Projects.Entities
{
    [Table(nameof(ProjectDeadlineNotification), Schema = DbSchemas.Construction)]
    [Index(nameof(ProjectId), nameof(NotificationType), nameof(EndedAt), nameof(ToAddress), IsUnique = true, Name = UniqueSentIndexName)]
    [Index(nameof(CreatedDate), Name = "IX_ProjectDeadlineNotification_CreatedDate")]
    public class ProjectDeadlineNotification
    {
        public const string UniqueSentIndexName = "IX_ProjectDeadlineNotification_Unique";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int ProjectId { get; private set; }
        [MaxLength(500)]
        public string ProjectName { get; private set; } = String.Empty;
        public int NotificationType { get; private set; }
        public DateTime EndedAt { get; private set; }
        public int DaysLeft { get; private set; }
        [MaxLength(256)]
        public string ToAddress { get; private set; } = String.Empty;
        [MaxLength(500)]
        public string Subject { get; private set; } = String.Empty;
        public bool IsSuccess { get; private set; }
        [MaxLength(2000)]
        public string? ErrorMessage { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }

        private ProjectDeadlineNotification()
        {
        }

        public static ProjectDeadlineNotification Create(int projectId, string projectName, int notificationType, DateTime endedAt, int daysLeft,
            string toAddress, string subject, string? createdBy, DateTime createdDate)
        {
            return new ProjectDeadlineNotification
            {
                ProjectId = projectId,
                ProjectName = projectName,
                NotificationType = notificationType,
                EndedAt = endedAt,
                DaysLeft = daysLeft,
                ToAddress = toAddress,
                Subject = subject,
                IsSuccess = true,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void MarkFailed(string errorMessage)
        {
            IsSuccess = false;
            ErrorMessage = errorMessage.Length > 2000 ? errorMessage[..2000] : errorMessage;
        }
    }
}
