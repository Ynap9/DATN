using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Db;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.Entities
{
    [Table(nameof(Project), Schema = DbSchemas.Construction)]
    [Index(nameof(Name), Name = $"IX_{nameof(Project)}_{nameof(Name)}")]
    public class Project
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public string Name { get; private set; } = String.Empty;
        public bool IsDeleted { get; private set; }
        public DateTime? StartedAt { get; private set; }
        public DateTime? EndedAt { get; private set; }
        public int Status { get; private set; }
        public string UrlGoogleSheetSync { get; private set; } = String.Empty;
        public string GoogleSheetName { get; private set; } = String.Empty;
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public string? DeletedBy { get; private set; }
        public DateTime? DeletedDate { get; private set; }

        private Project()
        {
        }

        public static Project Create(string name, DateTime? startedAt, DateTime? endedAt, string? createdBy, DateTime? createdDate)
        {
            EnsureValidPeriod(startedAt, endedAt);

            return new Project
            {
                Name = name,
                StartedAt = startedAt,
                EndedAt = endedAt,
                Status = ProjectConst.DEPLOYING,
                GoogleSheetName = name,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void LinkGoogleSheet(string urlGoogleSheetSync)
        {
            UrlGoogleSheetSync = urlGoogleSheetSync;
        }

        public void Update(string name, DateTime? startedAt, DateTime? endedAt, int status, string? modifiedBy, DateTime modifiedDate)
        {
            EnsureValidStatus(status);
            EnsureValidPeriod(startedAt, endedAt);

            Name = name;
            StartedAt = startedAt;
            EndedAt = endedAt;
            Status = status;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Delete(string? deletedBy, DateTime deletedDate)
        {
            IsDeleted = true;
            DeletedDate = deletedDate;
            DeletedBy = deletedBy;
        }

        private static void EnsureValidStatus(int status)
        {
            if (!ProjectConst.STATUSES.Contains(status))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Trạng thái dự án không hợp lệ");
            }
        }

        private static void EnsureValidPeriod(DateTime? startedAt, DateTime? endedAt)
        {
            if (startedAt > endedAt)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Ngày bắt đầu không được sau ngày kết thúc");
            }
        }
    }
}
