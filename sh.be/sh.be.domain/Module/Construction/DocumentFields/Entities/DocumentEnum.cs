using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.DocumentFields.Entities
{
    [Table(nameof(DocumentEnum), Schema = DbSchemas.Construction)]
    [Index(nameof(DocumentTypeId), nameof(Name), IsUnique = true, Name = UniqueNameIndexName)]
    public class DocumentEnum : ISoftDeleted
    {
        public const string UniqueNameIndexName = $"IX_{nameof(DocumentEnum)}_{nameof(DocumentTypeId)}_{nameof(Name)}";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int DocumentTypeId { get; private set; }
        [MaxLength(200)]
        public string Name { get; private set; } = String.Empty;
        public string ValuesJson { get; private set; } = "[]";
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private DocumentEnum()
        {
        }

        public static DocumentEnum Create(int documentTypeId, string name, string valuesJson, string? createdBy, DateTime createdDate)
        {
            return new DocumentEnum
            {
                DocumentTypeId = documentTypeId,
                Name = name,
                ValuesJson = valuesJson,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void Update(string name, string valuesJson, string? modifiedBy, DateTime modifiedDate)
        {
            Name = name;
            ValuesJson = valuesJson;
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
