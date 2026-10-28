using sh.be.shared.Constants.Operation;
using System.ComponentModel.DataAnnotations;

namespace sh.be.applications.Module.Operation.Dtos.ProjectMember
{
    public class AddProjectLeaderDto
    {
        [Required]
        public int DocumentTypeId { get; set; }
        [Required]
        [MinLength(1)]
        [MaxLength(ProjectMemberConst.USERS_PER_REQUEST_MAX)]
        public List<string> UserIds { get; set; } = new();
    }
}
