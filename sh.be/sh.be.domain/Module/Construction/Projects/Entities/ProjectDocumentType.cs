using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;
using sh.be.domain.Module.Construction.DocumentFields.Entities;

namespace sh.be.domain.Module.Construction.Projects.Entities
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

        public List<ProjectDocumentTypeDocumentField> AssignDocumentFields(IEnumerable<DocumentField> documentFields, string? assignedBy, DateTime? assignedDate)
        {
            return documentFields
                .Where(f => f.DocumentTypeId == DocumentTypeId)
                .OrderBy(f => f.OrderTableView)
                .ThenBy(f => f.Id)
                .Select((f, index) => ProjectDocumentTypeDocumentField.Create(ProjectId, DocumentTypeId, f.Id, f.FieldImport ?? String.Empty,
                    index + 1, assignedBy, assignedDate))
                .ToList();
        }
    }
}
