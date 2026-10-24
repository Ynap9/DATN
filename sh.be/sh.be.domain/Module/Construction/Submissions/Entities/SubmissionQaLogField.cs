using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.Submissions.Entities
{
    [Table(nameof(SubmissionQaLogField), Schema = DbSchemas.Construction)]
    public class SubmissionQaLogField : ISoftDeleted
    {
        public int SubmissionQaLogId { get; private set; }
        public int DocumentFieldId { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private SubmissionQaLogField()
        {
        }

        public static SubmissionQaLogField Create(int submissionQaLogId, int documentFieldId, string? createdBy, DateTime createdDate)
        {
            return new SubmissionQaLogField
            {
                SubmissionQaLogId = submissionQaLogId,
                DocumentFieldId = documentFieldId,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }
    }
}
