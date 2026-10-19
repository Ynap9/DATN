using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Db;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.Entities
{
    [Table(nameof(Submission), Schema = DbSchemas.Construction)]
    [Index(nameof(FileName), Name = $"IX_{nameof(Submission)}_{nameof(FileName)}")]
    [Index(nameof(ProjectId), nameof(DocumentTypeId), nameof(StatusCheck), Name = "IX_Submission_ProjectId_DocumentTypeId_StatusCheck")]
    [Index(nameof(ProjectId), nameof(DocumentTypeId), nameof(FileName), nameof(ProfileRange), nameof(Box), IsUnique = true,
        Name = UniqueKeyIndexName)]
    public class Submission : ISoftDeleted
    {
        public const string UniqueKeyIndexName = $"IX_{nameof(Submission)}_{nameof(ProjectId)}_{nameof(DocumentTypeId)}_{nameof(FileName)}_{nameof(ProfileRange)}_{nameof(Box)}";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int ProjectId { get; private set; }
        public int DocumentTypeId { get; private set; }
        [MaxLength(500)]
        public string Name { get; private set; } = String.Empty;
        [MaxLength(255)]
        public string FileName { get; private set; } = String.Empty;
        [MaxLength(1000)]
        public string FileUrlGoogleDrive { get; private set; } = String.Empty;
        [MaxLength(1000)]
        public string FileUrlGstore { get; private set; } = String.Empty;
        [MaxLength(1000)]
        public string FileUrlS3Object { get; private set; } = String.Empty;
        [MaxLength(100)]
        public string ProfileRange { get; private set; } = String.Empty;
        [MaxLength(100)]
        public string Box { get; private set; } = String.Empty;
        public int StatusCheck { get; private set; } = SubmissionConst.NOT_CHECKED;
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private Submission()
        {
        }

        public static Submission Create(int projectId, int documentTypeId, string fileName, string profileRange, string box, string fileUrl,
            string? createdBy, DateTime createdDate)
        {
            var trimmedFileName = fileName.Trim();
            var trimmedFileUrl = fileUrl.Trim();

            return new Submission
            {
                ProjectId = projectId,
                DocumentTypeId = documentTypeId,
                Name = trimmedFileName,
                FileName = trimmedFileName,
                ProfileRange = profileRange.Trim(),
                Box = box.Trim(),
                FileUrlGoogleDrive = trimmedFileUrl,
                FileUrlGstore = trimmedFileUrl,
                FileUrlS3Object = trimmedFileUrl,
                StatusCheck = SubmissionConst.NOT_CHECKED,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public SubmissionData CreateData(int documentFieldId, string data, int indexRow, string indexGoogleSheet,
            string? createdBy, DateTime createdDate)
        {
            return SubmissionData.Create(Id, ProjectId, DocumentTypeId, documentFieldId, data, indexRow, indexGoogleSheet,
                createdBy, createdDate);
        }

        public bool IsChecked()
        {
            return StatusCheck == SubmissionConst.CHECKED;
        }

        public SubmissionLog Check(string checkedBy, string userName, string? note, int changedDataCount, DateTime checkedDate)
        {
            var trimmedNote = note?.Trim();
            if (trimmedNote?.Length > SubmissionConst.CHECK_NOTE_MAX_LENGTH)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Ghi chú tối đa {SubmissionConst.CHECK_NOTE_MAX_LENGTH} ký tự");
            }

            var oldStatus = StatusCheck;
            var description = oldStatus == SubmissionConst.CHECKED
                ? $"Người dùng: {userName} đã tái kiểm tra hồ sơ."
                : $"Người dùng: {userName} đã kiểm tra hồ sơ.";

            StatusCheck = SubmissionConst.CHECKED;
            ModifiedBy = checkedBy;
            ModifiedDate = checkedDate;

            return SubmissionLog.Create(Id, ProjectId, DocumentTypeId, checkedBy, oldStatus, StatusCheck, description,
                String.IsNullOrEmpty(trimmedNote) ? null : trimmedNote, changedDataCount, checkedBy, checkedDate);
        }

        public void EnsureBelongsTo(int projectId, int documentTypeId)
        {
            if (ProjectId != projectId || DocumentTypeId != documentTypeId)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotInDocumentType);
            }
        }

        public void Delete(string? deletedBy, DateTime deletedDate)
        {
            Deleted = true;
            DeletedDate = deletedDate;
            DeletedBy = deletedBy;
        }
    }
}
