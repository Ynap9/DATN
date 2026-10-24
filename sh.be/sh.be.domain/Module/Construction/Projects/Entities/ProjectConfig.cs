using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Db;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.Projects.Entities
{
    [Table(nameof(ProjectConfig), Schema = DbSchemas.Construction)]
    [Index(nameof(Name), Name = $"IX_{nameof(ProjectConfig)}_{nameof(Name)}")]
    public class ProjectConfig : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public string Name { get; private set; } = String.Empty;
        public string Value { get; private set; } = String.Empty;
        public string Description { get; private set; } = String.Empty;
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private ProjectConfig()
        {
        }

        public static ProjectConfig Create(string name, string value, string? description, string? createdBy, DateTime? createdDate)
        {
            return new ProjectConfig
            {
                Name = EnsureValidName(name),
                Value = value,
                Description = description ?? String.Empty,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void Update(string name, string value, string? description, string? modifiedBy, DateTime modifiedDate)
        {
            Name = EnsureValidName(name);
            Value = IsSecret() && value == ProjectConfigConst.MASKED_VALUE ? Value : value;
            Description = description ?? String.Empty;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void ChangeValue(string value, string? modifiedBy, DateTime modifiedDate)
        {
            Value = value;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Delete(string? deletedBy, DateTime deletedDate)
        {
            Deleted = true;
            DeletedDate = deletedDate;
            DeletedBy = deletedBy;
        }

        public bool IsSecret()
        {
            return ProjectConfigConst.SECRET_NAMES.Contains(Name);
        }

        private static string EnsureValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Tên cấu hình không được để trống");
            }
            return name.Trim();
        }
    }
}
