using System.ComponentModel.DataAnnotations;

namespace sh.be.applications.Module.Operation.Dtos.Assignment
{
    public class AssignmentRequestDto
    {
        [Required]
        public string Role { get; set; } = String.Empty;
        [Required]
        public string AssigneeUserId { get; set; } = String.Empty;
        [Required]
        [MinLength(1)]
        public List<AssignmentItemDto> Items { get; set; } = new();
    }
}
