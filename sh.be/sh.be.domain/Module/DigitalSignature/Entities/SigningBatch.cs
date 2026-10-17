using sh.be.shared.Constants.Db;
using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.DigitalSignature.Entities
{
    [Table(nameof(SigningBatch), Schema = DbSchemas.DigitalSignature)]
    public class SigningBatch : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int TemplateId { get; private set; }
        public int? ProjectId { get; private set; }
        [MaxLength(500)]
        public string? ProjectName { get; private set; }
        [MaxLength(100)]
        public string? CertificateThumbprint { get; private set; }
        public int Status { get; private set; }
        public int TotalFiles { get; private set; }
        public int SignedFiles { get; private set; }
        public int FailedFiles { get; private set; }
        [MaxLength(1000)]
        public string? StopReason { get; private set; }
        public DateTime? StartedAt { get; private set; }
        public DateTime? FinishedAt { get; private set; }
        [MaxLength(500)]
        public string? SignedFolderName { get; private set; }
        public bool IsProjectNotified { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private SigningBatch()
        {
        }

        public static SigningBatch Create(int templateId, string? createdBy, DateTime? createdDate)
        {
            return new SigningBatch
            {
                TemplateId = templateId,
                Status = SigningBatchConst.NEW,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void NameSignedFolder(string? signedFolderName)
        {
            var normalizedName = new string((signedFolderName ?? String.Empty).Where(c => c != '/' && c != '\\' && !char.IsControl(c)).ToArray()).Trim();
            if (normalizedName.Length == 0)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSignedFolderNameRequired);
            }
            SignedFolderName = normalizedName;
        }

        public void EnsureOwnedBy(string? userId)
        {
            if (CreatedBy != userId)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningBatchNotOwned);
            }
        }

        public void LinkProject(int projectId, string projectName, string signedFolderName)
        {
            ProjectId = projectId;
            ProjectName = projectName;
            SignedFolderName = signedFolderName;
        }

        public void EnsureAcceptUpload()
        {
            if (ProjectId.HasValue)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorProjectBatchRejectsUpload);
            }
        }

        public SigningBatchFile CreateFile(int order, string fileName, string sourceObjectKey, string? documentId, string? createdBy, DateTime? createdDate)
        {
            if (Status != SigningBatchConst.NEW)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningBatchStarted);
            }

            TotalFiles++;
            return SigningBatchFile.Create(Id, order, fileName, sourceObjectKey, documentId, createdBy, createdDate);
        }

        public void RemoveFile(SigningBatchFile file, string? deletedBy, DateTime deletedDate)
        {
            if (Status == SigningBatchConst.SIGNING)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningBatchRunning);
            }

            if (file.Status == SigningBatchFileConst.FAILED)
            {
                FailedFiles = Math.Max(0, FailedFiles - 1);
            }
            file.Delete(deletedBy, deletedDate);
            TotalFiles = Math.Max(0, TotalFiles - 1);
            ModifiedBy = deletedBy;
            ModifiedDate = deletedDate;
        }

        public void ChangeCertificate(string certificateThumbprint, string? modifiedBy, DateTime modifiedDate)
        {
            if (!SigningBatchConst.SESSION_OPENABLE_STATUSES.Contains(Status))
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningBatchNotSignable);
            }

            CertificateThumbprint = certificateThumbprint;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void RequeueFailedFiles(int requeuedFiles)
        {
            FailedFiles = Math.Max(0, FailedFiles - requeuedFiles);
        }

        public void Start(string? sessionThumbprint, int pendingFiles, string? modifiedBy, DateTime modifiedDate)
        {
            if (!SigningBatchConst.SESSION_OPENABLE_STATUSES.Contains(Status))
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningBatchNotSignable);
            }
            if (string.IsNullOrWhiteSpace(CertificateThumbprint))
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorCertificateRequired);
            }
            if (sessionThumbprint == null)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningSessionNotOpen);
            }
            if (!string.Equals(sessionThumbprint, CertificateThumbprint, StringComparison.OrdinalIgnoreCase))
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningCertificateMismatch);
            }
            if (pendingFiles == 0)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningBatchEmpty);
            }

            Status = SigningBatchConst.SIGNING;
            StopReason = null;
            FinishedAt = null;
            StartedAt ??= modifiedDate;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Pause(string? stopReason, string? modifiedBy, DateTime modifiedDate)
        {
            if (Status != SigningBatchConst.SIGNING)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningBatchNotRunning);
            }

            Status = SigningBatchConst.PAUSED;
            StopReason = stopReason;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Cancel(string? modifiedBy, DateTime modifiedDate)
        {
            if (SigningBatchConst.FINISHED_STATUSES.Contains(Status))
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningBatchFinished);
            }

            Status = SigningBatchConst.CANCELLED;
            FinishedAt = modifiedDate;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Complete(string? modifiedBy, DateTime modifiedDate)
        {
            if (Status != SigningBatchConst.SIGNING)
            {
                return;
            }

            if (FailedFiles > 0)
            {
                Fail(SigningBatchConst.FAILED_FILES_REASON, modifiedBy, modifiedDate);
                return;
            }
            Finish(modifiedBy, modifiedDate);
        }

        public void Finish(string? modifiedBy, DateTime modifiedDate)
        {
            Status = SigningBatchConst.DONE;
            FinishedAt = modifiedDate;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Fail(string stopReason, string? modifiedBy, DateTime modifiedDate)
        {
            if (Status != SigningBatchConst.SIGNING)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningBatchNotRunning);
            }

            Status = SigningBatchConst.FAILED;
            StopReason = stopReason;
            FinishedAt = modifiedDate;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void MarkProjectNotified(string? modifiedBy, DateTime modifiedDate)
        {
            IsProjectNotified = true;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Delete(string? deletedBy, DateTime deletedDate)
        {
            Deleted = true;
            DeletedDate = deletedDate;
            DeletedBy = deletedBy;
        }
    }
}
