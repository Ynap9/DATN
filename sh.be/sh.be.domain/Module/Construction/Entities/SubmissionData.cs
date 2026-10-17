using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.Entities
{
    [Table(nameof(SubmissionData), Schema = DbSchemas.Construction)]
    [Index(nameof(SubmissionId), nameof(DocumentFieldId), IsUnique = true, Name = UniqueFieldIndexName)]
    [Index(nameof(ProjectId), nameof(DocumentTypeId), nameof(DocumentFieldId),
        Name = $"IX_{nameof(SubmissionData)}_{nameof(ProjectId)}_{nameof(DocumentTypeId)}_{nameof(DocumentFieldId)}")]
    public class SubmissionData : ISoftDeleted
    {
        public const string UniqueFieldIndexName = $"IX_{nameof(SubmissionData)}_{nameof(SubmissionId)}_{nameof(DocumentFieldId)}";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int SubmissionId { get; private set; }
        public int ProjectId { get; private set; }
        public int DocumentTypeId { get; private set; }
        public int DocumentFieldId { get; private set; }
        public string Data { get; private set; } = String.Empty;
        public int? IndexRow { get; private set; }
        [MaxLength(20)]
        public string? IndexGoogleSheet { get; private set; } = String.Empty;
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private SubmissionData()
        {
        }

        public static SubmissionData Create(int submissionId, int projectId, int documentTypeId, int documentFieldId, string data,
            int? indexRow, string? indexGoogleSheet, string? createdBy, DateTime createdDate)
        {
            return new SubmissionData
            {
                SubmissionId = submissionId,
                ProjectId = projectId,
                DocumentTypeId = documentTypeId,
                DocumentFieldId = documentFieldId,
                Data = data,
                IndexRow = indexRow,
                IndexGoogleSheet = indexGoogleSheet,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void AssignSubmission(int submissionId)
        {
            SubmissionId = submissionId;
        }

        public bool FillIfBlank(string newData, string? indexGoogleSheet, string? modifiedBy, DateTime modifiedDate)
        {
            var trimmedNewData = newData.Trim();
            if (!string.IsNullOrWhiteSpace(Data) || trimmedNewData.Length == 0)
            {
                return false;
            }

            Data = trimmedNewData;
            IndexGoogleSheet = indexGoogleSheet;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
            return true;
        }

        public bool Overwrite(string newData, string? modifiedBy, DateTime modifiedDate)
        {
            var trimmedNewData = newData.Trim();
            if (trimmedNewData.Length == 0 || trimmedNewData == Data.Trim())
            {
                return false;
            }

            Data = trimmedNewData;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
            return true;
        }

        public void Delete(string? deletedBy, DateTime deletedDate)
        {
            Deleted = true;
            DeletedDate = deletedDate;
            DeletedBy = deletedBy;
        }
    }
}
