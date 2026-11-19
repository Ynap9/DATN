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

        public void Restore(string? restoredBy, DateTime restoredDate)
        {
            Deleted = false;
            DeletedDate = null;
            DeletedBy = null;
            ModifiedBy = restoredBy;
            ModifiedDate = restoredDate;
        }

        // Khôi phục cấu hình cũ của các trường chưa xoá (giữ FieldImport, thứ tự); trường chưa có cấu hình thì trả dòng mới nối cuối thứ tự import
        public List<ProjectDocumentTypeDocumentField> RestoreDocumentFields(IEnumerable<ProjectDocumentTypeDocumentField> oldDocumentFields,
            IEnumerable<DocumentField> documentFields, string? restoredBy, DateTime restoredDate)
        {
            var activeDocumentFields = documentFields.Where(f => f.DocumentTypeId == DocumentTypeId).ToList();
            var activeDocumentFieldIds = activeDocumentFields.Select(f => f.Id).ToHashSet();
            var ownOldDocumentFields = oldDocumentFields.Where(pf => pf.ProjectId == ProjectId && pf.DocumentTypeId == DocumentTypeId).ToList();
            foreach (var oldDocumentField in ownOldDocumentFields.Where(pf => activeDocumentFieldIds.Contains(pf.DocumentFieldId)))
            {
                oldDocumentField.Restore(restoredBy, restoredDate);
            }

            var oldDocumentFieldIds = ownOldDocumentFields.Select(pf => pf.DocumentFieldId).ToHashSet();
            var nextOrder = ownOldDocumentFields.Select(pf => pf.OrderFieldImport).DefaultIfEmpty(0).Max() + 1;
            return activeDocumentFields
                .Where(f => !oldDocumentFieldIds.Contains(f.Id))
                .OrderBy(f => f.OrderTableView)
                .ThenBy(f => f.Id)
                .Select((f, index) => ProjectDocumentTypeDocumentField.Create(ProjectId, DocumentTypeId, f.Id, f.FieldImport ?? String.Empty,
                    nextOrder + index, restoredBy, restoredDate))
                .ToList();
        }
    }
}
