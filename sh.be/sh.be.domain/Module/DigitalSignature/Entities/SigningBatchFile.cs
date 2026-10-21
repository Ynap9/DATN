using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.DigitalSignature.Entities
{
    [Table(nameof(SigningBatchFile), Schema = DbSchemas.DigitalSignature)]
    [Index(nameof(SigningBatchId), nameof(Status), nameof(Order),
        Name = $"IX_{nameof(SigningBatchFile)}_{nameof(SigningBatchId)}_{nameof(Status)}_{nameof(Order)}")]
    public class SigningBatchFile : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int SigningBatchId { get; private set; }
        public int Order { get; private set; }
        [MaxLength(50)]
        public string? DocumentId { get; private set; }
        [MaxLength(500)]
        public string FileName { get; private set; } = String.Empty;
        [MaxLength(1000)]
        public string SourceObjectKey { get; private set; } = String.Empty;
        [MaxLength(1000)]
        public string? SignedObjectKey { get; private set; }
        public int Status { get; private set; }
        [MaxLength(1000)]
        public string? ErrorMessage { get; private set; }
        public DateTime? SignedAt { get; private set; }
        public DateTime? TimestampedAt { get; private set; }
        public bool? IsSignatureValid { get; private set; }
        [MaxLength(500)]
        public string? SignatureValidationMessage { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private SigningBatchFile()
        {
        }

        public static SigningBatchFile Create(int signingBatchId, int order, string fileName, string sourceObjectKey, string? documentId,
            string? createdBy, DateTime? createdDate)
        {
            return new SigningBatchFile
            {
                SigningBatchId = signingBatchId,
                Order = order,
                FileName = fileName,
                SourceObjectKey = sourceObjectKey,
                DocumentId = documentId,
                Status = SigningBatchFileConst.PENDING,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void StartSigning(string? modifiedBy, DateTime modifiedDate)
        {
            Status = SigningBatchFileConst.SIGNING;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void MarkSigned(string signedObjectKey, DateTime signedAt, DateTime? timestampedAt, bool isSignatureValid,
            string? signatureValidationMessage, string? modifiedBy, DateTime modifiedDate)
        {
            Status = SigningBatchFileConst.DONE;
            SignedObjectKey = signedObjectKey;
            SignedAt = signedAt;
            TimestampedAt = timestampedAt;
            IsSignatureValid = isSignatureValid;
            SignatureValidationMessage = signatureValidationMessage;
            ErrorMessage = null;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void MarkFailed(string errorMessage, bool? isSignatureValid, string? signatureValidationMessage,
            string? modifiedBy, DateTime modifiedDate)
        {
            Status = SigningBatchFileConst.FAILED;
            ErrorMessage = errorMessage;
            if (isSignatureValid.HasValue)
            {
                IsSignatureValid = isSignatureValid;
                SignatureValidationMessage = signatureValidationMessage;
            }
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void ReturnToQueue(string? modifiedBy, DateTime modifiedDate)
        {
            Status = SigningBatchFileConst.PENDING;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Retry(string? modifiedBy, DateTime modifiedDate)
        {
            Status = SigningBatchFileConst.PENDING;
            ErrorMessage = null;
            IsSignatureValid = null;
            SignatureValidationMessage = null;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public string GetSignedObjectKey()
        {
            if (Status != SigningBatchFileConst.DONE || string.IsNullOrWhiteSpace(SignedObjectKey))
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSignedFileNotReady);
            }
            return SignedObjectKey;
        }

        public void Delete(string? deletedBy, DateTime deletedDate)
        {
            if (Status != SigningBatchFileConst.PENDING && Status != SigningBatchFileConst.FAILED)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorSigningBatchFileNotDeletable);
            }

            Deleted = true;
            DeletedDate = deletedDate;
            DeletedBy = deletedBy;
        }
    }
}
