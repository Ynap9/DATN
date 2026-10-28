using sh.be.shared.Constants.Operation;
using System.ComponentModel.DataAnnotations;

namespace sh.be.applications.Module.Operation.Dtos.ProjectMember
{
    public class AddProjectMemberDto
    {
        [Required]
        public int DocumentTypeId { get; set; }
        public string? ManagedByUserId { get; set; }
        [MaxLength(ProjectMemberConst.USERS_PER_REQUEST_MAX)]
        public List<AddMemberItemDto> Users { get; set; } = new();
        public List<int> StaffGroupIds { get; set; } = new();
    }
}
