using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.Entities
{
    [Table(nameof(ProjectDocumentType), Schema = DbSchemas.Construction)]
    [Index(nameof(DocumentTypeId), Name = $"IX_{nameof(ProjectDocumentType)}_{nameof(DocumentTypeId)}")]
    public class ProjectDocumentType : ISoftDeleted
    {
        public int ProjectId { get; private set; }
        public int DocumentTypeId { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private ProjectDocumentType()
        {
        }

        public static ProjectDocumentType Create(int projectId, int documentTypeId, string? createdBy, DateTime? createdDate)
        {
            return new ProjectDocumentType
            {
                ProjectId = projectId,
                DocumentTypeId = documentTypeId,
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
