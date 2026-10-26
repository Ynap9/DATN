using sh.be.shared.Constants.Operation;
using System.ComponentModel.DataAnnotations;

namespace sh.be.applications.Module.Operation.Dtos.StaffGroup
{
    public class ChangeStaffGroupMembersDto
    {
        [Required]
        [MinLength(1)]
        [MaxLength(StaffGroupConst.MEMBERS_PER_REQUEST_MAX)]
        public List<string> UserIds { get; set; } = new();
    }
}
