using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.Entities
{
    [Table(nameof(DocumentType), Schema = DbSchemas.Construction)]
    [Index(nameof(Name), Name = $"IX_{nameof(DocumentType)}_{nameof(Name)}")]
    public class DocumentType : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int DefaultView { get; private set; } = 1;
        public string Name { get; private set; } = String.Empty;
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private DocumentType()
        {
        }

        public static DocumentType Create(string name, string? createdBy, DateTime? createdDate, int defaultView = 1)
        {
            return new DocumentType
            {
                Name = name,
                DefaultView = defaultView,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void Update(string name, int defaultView, string? modifiedBy, DateTime modifiedDate)
        {
            Name = name;
            DefaultView = defaultView;
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
