using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Operation.StaffGroups.Entities
{
    [Table(nameof(StaffGroupMember), Schema = DbSchemas.Operation)]
    [Index(nameof(StaffGroupId), nameof(UserId), IsUnique = true, Name = $"IX_{nameof(StaffGroupMember)}_{nameof(StaffGroupId)}_{nameof(UserId)}")]
    public class StaffGroupMember
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int StaffGroupId { get; private set; }
        [MaxLength(450)]
        public string UserId { get; private set; } = String.Empty;
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }

        private StaffGroupMember()
        {
        }

        public static StaffGroupMember Create(int staffGroupId, string userId, string? createdBy, DateTime createdDate)
        {
            return new StaffGroupMember
            {
                StaffGroupId = staffGroupId,
                UserId = userId,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }
    }
}
