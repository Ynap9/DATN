using sh.be.shared.Constants.Db;
using sh.be.shared.Constants.Packaging;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace sh.be.domain.Module.Packaging.Entities
{
    [Table(nameof(PackageSession), Schema = DbSchemas.Packaging)]
    public class PackageSession : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int PackageType { get; private set; }
        [MaxLength(20)]
        public string MetadataTypes { get; private set; } = String.Empty;
        [MaxLength(500)]
        public string? RootFolderName { get; private set; }
        [MaxLength(PackageSessionConst.STATUS_MAX_LENGTH)]
        public string Status { get; private set; } = String.Empty;
        public int TotalDocument { get; private set; }
        public int DoneDocument { get; private set; }
        public int ErrorDocument { get; private set; }
        public int WarningDocument { get; private set; }
        public DateTime? StartedDate { get; private set; }
        public DateTime? FinishedDate { get; private set; }
        [MaxLength(2000)]
        public string? FailReason { get; private set; }
        public string? MatchReport { get; private set; }
        public DateTime ExpiredDate { get; private set; }
        [MaxLength(PackageSessionConst.STATUS_MAX_LENGTH)]
        public string? BuildStatus { get; private set; }
        public int BuildTotal { get; private set; }
        public int BuildDone { get; private set; }
        [MaxLength(2000)]
        public string? BuildFailReason { get; private set; }
        [MaxLength(50)]
        public string? FondsCode { get; private set; }
        [MaxLength(256)]
        public string? BuiltBy { get; private set; }
        [MaxLength(PackageBuildConst.ARCHIVE_MAX_LENGTH)]
        public string? ArchiveCode { get; private set; }
        [MaxLength(PackageBuildConst.ARCHIVE_MAX_LENGTH)]
        public string? ArchiveName { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private PackageSession()
        {
        }

        public static PackageSession Create(int packageType, List<int> metadataTypes, string? createdBy, DateTime createdDate)
        {
            return new PackageSession
            {
                PackageType = packageType,
                MetadataTypes = string.Join(",", metadataTypes.Order()),
                Status = PackageSessionConst.PENDING,
                ExpiredDate = createdDate.AddDays(PackageSessionConst.RETENTION_DAYS),
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public List<int> GetMetadataTypes()
        {
            return MetadataTypes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
        }

        public void AcceptUpload(string rootFolderName, string? modifiedBy, DateTime modifiedDate)
        {
            if (Status != PackageSessionConst.PENDING)
            {
                throw new UserFriendlyException(ErrorCodes.PackagingErrorSessionNotPending);
            }

            RootFolderName ??= rootFolderName;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Start(int excelCount, int documentCount, string? modifiedBy, DateTime modifiedDate)
        {
            if (Status != PackageSessionConst.PENDING || excelCount == 0 || documentCount == 0)
            {
                throw new UserFriendlyException(ErrorCodes.PackagingErrorSessionStartInvalid);
            }

            Status = PackageSessionConst.RUNNING;
            TotalDocument = documentCount;
            DoneDocument = 0;
            ErrorDocument = 0;
            WarningDocument = 0;
            StartedDate = modifiedDate;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void SaveMatchReport(string matchReport)
        {
            MatchReport = matchReport;
        }

        public void AddProgress(int doneDocument, int errorDocument, int warningDocument, string? modifiedBy, DateTime modifiedDate)
        {
            DoneDocument += doneDocument;
            ErrorDocument += errorDocument;
            WarningDocument += warningDocument;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Finish(List<PackageDossier> dossiers, bool hasErrorOutsideDossier, string? modifiedBy, DateTime modifiedDate)
        {
            Status = dossiers.Count == 0 || hasErrorOutsideDossier || dossiers.Any(d => d.Status == PackageSessionConst.FAILED)
                ? PackageSessionConst.FAILED
                : dossiers.Any(d => d.Status == PackageSessionConst.WARNING) ? PackageSessionConst.WARNING : PackageSessionConst.PASSED;
            FinishedDate = modifiedDate;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Fail(string failReason, string? modifiedBy, DateTime modifiedDate)
        {
            Status = PackageSessionConst.FAILED;
            FailReason = failReason.Length > 2000 ? failReason[..2000] : failReason;
            FinishedDate = modifiedDate;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Cancel(string? modifiedBy, DateTime modifiedDate)
        {
            if (!PackageSessionConst.CANCELLABLE_STATUSES.Contains(Status))
            {
                throw new UserFriendlyException(ErrorCodes.PackagingErrorSessionFinished);
            }

            Status = PackageSessionConst.CANCELLED;
            FinishedDate = modifiedDate;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        // Mã phông bắt buộc (nằm trong OBJID urn nên cấm ":"); mã + tên cơ quan lưu trữ trim, chuỗi trắng = không nhập, phải đủ cặp hoặc bỏ cả hai
        public void StartBuild(string? fondsCode, string? archiveCode, string? archiveName, int buildTotal, string builtBy, string? modifiedBy, DateTime modifiedDate)
        {
            if (!PackageBuildConst.BUILDABLE_STATUSES.Contains(Status) || buildTotal == 0)
            {
                throw new UserFriendlyException(ErrorCodes.PackagingErrorBuildInvalid);
            }
            if (BuildStatus == PackageSessionConst.RUNNING)
            {
                throw new UserFriendlyException(ErrorCodes.PackagingErrorBuildRunning);
            }

            var normalizedFondsCode = fondsCode?.Trim() ?? String.Empty;
            if (normalizedFondsCode.Length == 0 || normalizedFondsCode.Length > PackageBuildConst.FONDS_CODE_MAX_LENGTH
                || !Regex.IsMatch(normalizedFondsCode, PackageBuildConst.FONDS_CODE_PATTERN))
            {
                throw new UserFriendlyException(ErrorCodes.PackagingErrorFondsCodeInvalid);
            }
            var normalizedArchiveCode = String.IsNullOrWhiteSpace(archiveCode) ? null : archiveCode.Trim();
            var normalizedArchiveName = String.IsNullOrWhiteSpace(archiveName) ? null : archiveName.Trim();
            if ((normalizedArchiveCode == null) != (normalizedArchiveName == null)
                || normalizedArchiveCode?.Length > PackageBuildConst.ARCHIVE_MAX_LENGTH
                || normalizedArchiveName?.Length > PackageBuildConst.ARCHIVE_MAX_LENGTH)
            {
                throw new UserFriendlyException(ErrorCodes.PackagingErrorArchiveInvalid);
            }

            FondsCode = normalizedFondsCode;
            ArchiveCode = normalizedArchiveCode;
            ArchiveName = normalizedArchiveName;
            BuiltBy = builtBy;
            BuildStatus = PackageSessionConst.RUNNING;
            BuildTotal = buildTotal;
            BuildDone = 0;
            BuildFailReason = null;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void AddBuildProgress(string? modifiedBy, DateTime modifiedDate)
        {
            BuildDone++;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void FinishBuild(List<PackageDossier> dossiers, string? modifiedBy, DateTime modifiedDate)
        {
            var hasPackage = dossiers.Any(d => d.PackageObjectKey != null);
            BuildStatus = hasPackage ? PackageBuildConst.DONE : PackageSessionConst.FAILED;
            BuildFailReason = hasPackage ? null : PackageBuildConst.ALL_PACKAGES_FAILED_MESSAGE;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void FailBuild(string failReason, string? modifiedBy, DateTime modifiedDate)
        {
            BuildStatus = PackageSessionConst.FAILED;
            BuildFailReason = failReason.Length > 2000 ? failReason[..2000] : failReason;
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
