using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using sh.be.shared.Constants.Packaging;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Packaging.Entities
{
    [Table(nameof(PackageDossier), Schema = DbSchemas.Packaging)]
    [Index(nameof(SessionId), Name = $"IX_{nameof(PackageDossier)}_{nameof(SessionId)}")]
    public class PackageDossier : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int SessionId { get; private set; }
        public int ExcelDocumentId { get; private set; }
        [MaxLength(100)]
        public string? PackageCode { get; private set; }
        public int RowIndex { get; private set; }
        [MaxLength(1000)]
        public string FolderName { get; private set; } = String.Empty;
        [MaxLength(PackageSessionConst.STATUS_MAX_LENGTH)]
        public string Status { get; private set; } = String.Empty;
        [MaxLength(4000)]
        public string? ErrorSummary { get; private set; }
        public int DocumentCount { get; private set; }
        [MaxLength(150)]
        public string? PackageObjid { get; private set; }
        [MaxLength(500)]
        public string? PackageObjectKey { get; private set; }
        [MaxLength(500)]
        public string? PackageFileName { get; private set; }
        public long? PackageSize { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private PackageDossier()
        {
        }

        public static PackageDossier Create(int sessionId, int excelDocumentId, string? packageCode, int rowIndex, string folderName, string? createdBy, DateTime createdDate)
        {
            return new PackageDossier
            {
                SessionId = sessionId,
                ExcelDocumentId = excelDocumentId,
                PackageCode = packageCode?.Length > 100 ? packageCode[..100] : packageCode,
                RowIndex = rowIndex,
                FolderName = folderName,
                Status = PackageSessionConst.PENDING,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void Complete(List<string> errors, List<string> warnings, List<PackageDocument> documents, string? modifiedBy, DateTime modifiedDate)
        {
            var hasFailedDocument = documents.Any(d => d.Status == PackageSessionConst.FAILED);
            var hasWarningDocument = documents.Any(d => d.Status == PackageSessionConst.WARNING);
            Status = errors.Count > 0 || hasFailedDocument
                ? PackageSessionConst.FAILED
                : warnings.Count > 0 || hasWarningDocument ? PackageSessionConst.WARNING : PackageSessionConst.PASSED;
            var summary = string.Join("; ", errors.Concat(warnings));
            ErrorSummary = summary.Length == 0 ? null : summary.Length > 4000 ? summary[..4000] : summary;
            DocumentCount = documents.Count(d => d.FileRole == PackageSessionConst.FILE_ROLE_DATA);
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        // Dựng lại: bỏ ZIP cũ và lỗi dựng lần trước (đứng sau BUILD_ERROR_PREFIX), giữ nguyên lỗi / cảnh báo của bước kiểm
        public void ResetBuild(string? modifiedBy, DateTime modifiedDate)
        {
            PackageObjid = null;
            PackageObjectKey = null;
            PackageFileName = null;
            PackageSize = null;
            var buildErrorIndex = ErrorSummary?.IndexOf(PackageBuildConst.BUILD_ERROR_PREFIX, StringComparison.Ordinal) ?? -1;
            if (buildErrorIndex >= 0)
            {
                var verifySummary = ErrorSummary![..buildErrorIndex].TrimEnd(' ', ';');
                ErrorSummary = verifySummary.Length == 0 ? null : verifySummary;
            }
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void CompleteBuild(string packageObjid, string packageObjectKey, string packageFileName, long packageSize, string? modifiedBy, DateTime modifiedDate)
        {
            PackageObjid = packageObjid;
            PackageObjectKey = packageObjectKey;
            PackageFileName = packageFileName;
            PackageSize = packageSize;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        // Lỗi dựng nối sau lỗi / cảnh báo bước kiểm; cắt bớt phần cũ để lỗi dựng không bị mất khi chạm trần 4000 ký tự
        public void FailBuild(string reason, string? modifiedBy, DateTime modifiedDate)
        {
            var buildError = $"{PackageBuildConst.BUILD_ERROR_PREFIX}{(reason.Length > PackageBuildConst.BUILD_ERROR_MAX_LENGTH ? reason[..PackageBuildConst.BUILD_ERROR_MAX_LENGTH] : reason)}";
            var maxVerifyLength = 4000 - buildError.Length - 2;
            ErrorSummary = ErrorSummary == null
                ? buildError
                : $"{(ErrorSummary.Length > maxVerifyLength ? ErrorSummary[..maxVerifyLength] : ErrorSummary)}; {buildError}";
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
