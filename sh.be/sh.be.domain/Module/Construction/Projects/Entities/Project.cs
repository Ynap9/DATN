using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Db;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.Projects.Entities
{
    [Table(nameof(Project), Schema = DbSchemas.Construction)]
    [Index(nameof(Name), Name = $"IX_{nameof(Project)}_{nameof(Name)}")]
    public class Project : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public string Name { get; private set; } = String.Empty;
        public bool Deleted { get; private set; }
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
            // Vào / ra nghiệm thu và sang Xong chỉ qua chức năng nghiệm thu để luôn kiểm điều kiện và ghi audit nghiệm thu
            if (status != Status && (Status == ProjectConst.ACCEPTING || status == ProjectConst.ACCEPTING || status == ProjectConst.DONE))
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectStatusTransitionInvalid, "Dùng chức năng nghiệm thu");
            }

            Name = name;
            StartedAt = startedAt;
            EndedAt = endedAt;
            Status = status;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Delete(string? deletedBy, DateTime deletedDate)
        {
            Deleted = true;
            DeletedDate = deletedDate;
            DeletedBy = deletedBy;
        }

        public bool Expire(DateTime today, string expiredBy, DateTime expiredDate)
        {
            if (!ProjectConst.ACTIVE_STATUSES.Contains(Status) || !EndedAt.HasValue || EndedAt.Value.Date >= today.Date)
            {
                return false;
            }

            Status = ProjectConst.EXPIRED;
            ModifiedBy = expiredBy;
            ModifiedDate = expiredDate;
            return true;
        }

        public void StartAcceptance(string? modifiedBy, DateTime modifiedDate)
        {
            TransitionStatus(ProjectConst.ACCEPTANCE_START_STATUSES, ProjectConst.ACCEPTING, modifiedBy, modifiedDate);
        }

        public void FinishAcceptance(string? modifiedBy, DateTime modifiedDate)
        {
            TransitionStatus([ProjectConst.ACCEPTING], ProjectConst.DONE, modifiedBy, modifiedDate);
        }

        public void ReopenFromAcceptance(string? modifiedBy, DateTime modifiedDate)
        {
            TransitionStatus([ProjectConst.ACCEPTING], ProjectConst.DOING, modifiedBy, modifiedDate);
        }

        public void EnsureImportable()
        {
            if (Status == ProjectConst.ACCEPTING)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectAccepting);
            }
            if (Status == ProjectConst.DONE)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectDone);
            }
            if (Status == ProjectConst.EXPIRED)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectExpired);
            }
        }

        public void EnsureEditable(bool isSuperAdmin)
        {
            if (Status == ProjectConst.DONE && !isSuperAdmin)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectDoneNotEditable);
            }
            if (Status == ProjectConst.EXPIRED && !isSuperAdmin)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectExpired);
            }
        }

        public List<ProjectDocumentType> AssignDocumentTypes(IEnumerable<int> documentTypeIds, string? assignedBy, DateTime? assignedDate)
        {
            return documentTypeIds
                .Select(documentTypeId => ProjectDocumentType.Create(Id, documentTypeId, assignedBy, assignedDate))
                .ToList();
        }

        private void TransitionStatus(IReadOnlyList<int> fromStatuses, int toStatus, string? modifiedBy, DateTime modifiedDate)
        {
            if (!fromStatuses.Contains(Status))
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectStatusTransitionInvalid);
            }

            Status = toStatus;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
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
