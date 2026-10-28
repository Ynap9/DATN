using System.ComponentModel.DataAnnotations;

namespace sh.be.applications.Module.Operation.Dtos.ProjectMember
{
    public class AddMemberItemDto
    {
        [Required]
        public string UserId { get; set; } = String.Empty;
        [Required]
        public string Role { get; set; } = String.Empty;
    }
}
