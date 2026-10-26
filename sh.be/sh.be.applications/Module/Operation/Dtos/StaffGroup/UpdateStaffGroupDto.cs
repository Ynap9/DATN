using sh.be.shared.Constants.Operation;
using System.ComponentModel.DataAnnotations;

namespace sh.be.applications.Module.Operation.Dtos.StaffGroup
{
    public class UpdateStaffGroupDto
    {
        [Required]
        [MaxLength(StaffGroupConst.NAME_MAX_LENGTH)]
        public string Name { get; set; } = String.Empty;
        [MaxLength(StaffGroupConst.DESCRIPTION_MAX_LENGTH)]
        public string? Description { get; set; }
    }
}
