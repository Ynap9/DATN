using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using sh.be.shared.Constants.Packaging;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Packaging.Entities
{
    [Table(nameof(PackageDocument), Schema = DbSchemas.Packaging)]
    [Index(nameof(SessionId), Name = $"IX_{nameof(PackageDocument)}_{nameof(SessionId)}")]
    public class PackageDocument : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int SessionId { get; private set; }
        public int? DossierId { get; private set; }
        [MaxLength(PackageSessionConst.STATUS_MAX_LENGTH)]
        public string FileRole { get; private set; } = String.Empty;
        public int? MetadataType { get; private set; }
        [MaxLength(1000)]
        public string RelativePath { get; private set; } = String.Empty;
        [MaxLength(500)]
        public string FileName { get; private set; } = String.Empty;
        [MaxLength(10)]
        public string Extension { get; private set; } = String.Empty;
        [MaxLength(100)]
        public string MimeType { get; private set; } = String.Empty;
        [MaxLength(500)]
        public string ObjectKey { get; private set; } = String.Empty;
        public long SizeBytes { get; private set; }
        [MaxLength(64)]
        public string Sha256 { get; private set; } = String.Empty;
        [MaxLength(100)]
        public string? DocKey { get; private set; }
        [MaxLength(PackageSessionConst.STATUS_MAX_LENGTH)]
        public string Status { get; private set; } = String.Empty;
        [MaxLength(2000)]
        public string? Reason { get; private set; }
        public bool IsSigned { get; private set; }
        [MaxLength(10)]
        public string? PdfAPart { get; private set; }
        public int? PageCount { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private PackageDocument()
        {
        }

        public static PackageDocument Create(int sessionId, string fileRole, string relativePath, string fileName, string extension, string mimeType, string objectKey,
            long sizeBytes, string sha256, bool isSigned, string? pdfAPart, int? pageCount, string? createdBy, DateTime createdDate)
        {
            return new PackageDocument
            {
                SessionId = sessionId,
                FileRole = fileRole,
                RelativePath = relativePath,
                FileName = fileName,
                Extension = extension,
                MimeType = mimeType,
                ObjectKey = objectKey,
                SizeBytes = sizeBytes,
                Sha256 = sha256,
                IsSigned = isSigned,
                PdfAPart = pdfAPart,
                PageCount = pageCount,
                Status = PackageSessionConst.PENDING,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void Complete(int? dossierId, string fileRole, int? metadataType, string? docKey, List<string> errors, List<string> warnings, string? modifiedBy, DateTime modifiedDate)
        {
            DossierId = dossierId;
            FileRole = fileRole;
            MetadataType = metadataType;
            DocKey = docKey?.Length > 100 ? docKey[..100] : docKey;
            Status = errors.Count > 0
                ? PackageSessionConst.FAILED
                : warnings.Count > 0 ? PackageSessionConst.WARNING : PackageSessionConst.PASSED;
            var reason = string.Join("; ", errors.Concat(warnings));
            Reason = reason.Length == 0 ? null : reason.Length > 2000 ? reason[..2000] : reason;
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
