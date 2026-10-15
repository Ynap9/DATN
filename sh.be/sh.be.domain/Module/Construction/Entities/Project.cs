using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Db;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.Entities
{
    [Table(nameof(Project), Schema = DbSchemas.Construction)]
    public class Project
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public string Name { get; private set; } = String.Empty;
        public bool IsDeleted { get; private set; }
        public DateTime? StartedAt { get; private set; }
        public DateTime? EndedAt { get; private set; }
        public int Status { get; private set; } = ProjectConst.DOING;
        public string UrlGoogleSheetSync { get; private set; } = String.Empty;
        public string GoogleSheetName { get; private set; } = String.Empty;
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public string? DeletedBy { get; private set; }
        public DateTime? DeletedDate { get; private set; }

        private Project()
        {
        }

        public static Project Create(string name, DateTime? startedAt, DateTime? endedAt, int status,
            string urlGoogleSheetSync, string googleSheetName, string? createdBy, DateTime? createdDate)
        {
            return new Project
            {
                Name = name,
                StartedAt = startedAt,
                EndedAt = endedAt,
                Status = status,
                UrlGoogleSheetSync = urlGoogleSheetSync,
                GoogleSheetName = googleSheetName,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void Update(string name, DateTime? startedAt, DateTime? endedAt, int status,
            string urlGoogleSheetSync, string googleSheetName, string? modifiedBy, DateTime modifiedDate)
        {
            Name = name;
            StartedAt = startedAt;
            EndedAt = endedAt;
            Status = status;
            UrlGoogleSheetSync = urlGoogleSheetSync;
            GoogleSheetName = googleSheetName;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Delete(string? deletedBy, DateTime deletedDate)
        {
            IsDeleted = true;
            DeletedDate = deletedDate;
            DeletedBy = deletedBy;
        }
    }
}
