using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.Projects.Entities
{
    [Table(nameof(ProjectDocumentTypeDocumentField), Schema = DbSchemas.Construction)]
    [Index(nameof(DocumentFieldId), Name = $"IX_{nameof(ProjectDocumentTypeDocumentField)}_{nameof(DocumentFieldId)}")]
    public class ProjectDocumentTypeDocumentField : ISoftDeleted
    {
        public int ProjectId { get; private set; }
        public int DocumentTypeId { get; private set; }
        public int DocumentFieldId { get; private set; }
        public string FieldImport { get; private set; } = String.Empty;
        public int OrderFieldImport { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private ProjectDocumentTypeDocumentField()
        {
        }

        public static ProjectDocumentTypeDocumentField Create(int projectId, int documentTypeId, int documentFieldId, string fieldImport,
            int orderFieldImport, string? createdBy, DateTime? createdDate)
        {
            return new ProjectDocumentTypeDocumentField
            {
                ProjectId = projectId,
                DocumentTypeId = documentTypeId,
                DocumentFieldId = documentFieldId,
                FieldImport = fieldImport,
                OrderFieldImport = orderFieldImport,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void Delete(string? deletedBy, DateTime deletedDate)
        {
            Deleted = true;
            DeletedDate = deletedDate;
            DeletedBy = deletedBy;
        }
    }
}
