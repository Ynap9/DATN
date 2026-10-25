using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using sh.be.shared.Constants.Packaging;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Packaging.Entities
{
    [Table(nameof(PackageSchema), Schema = DbSchemas.Packaging)]
    [Index(nameof(PackageType), nameof(MetadataType), IsUnique = true, Name = UniqueTypeIndexName)]
    public class PackageSchema : ISoftDeleted
    {
        public const string UniqueTypeIndexName = $"IX_{nameof(PackageSchema)}_{nameof(PackageType)}_{nameof(MetadataType)}";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int PackageType { get; private set; }
        public int MetadataType { get; private set; }
        [MaxLength(PackageSchemaConst.NAME_MAX_LENGTH)]
        public string Name { get; private set; } = String.Empty;
        [MaxLength(50)]
        public string XsdFileName { get; private set; } = String.Empty;
        [MaxLength(20)]
        public string MetadataFilePrefix { get; private set; } = String.Empty;
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private PackageSchema()
        {
        }

        public static PackageSchema Create(int packageType, int metadataType, string name, string xsdFileName, string metadataFilePrefix,
            string? createdBy, DateTime? createdDate)
        {
            return new PackageSchema
            {
                PackageType = packageType,
                MetadataType = metadataType,
                Name = name,
                XsdFileName = xsdFileName,
                MetadataFilePrefix = metadataFilePrefix,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }
    }
}
