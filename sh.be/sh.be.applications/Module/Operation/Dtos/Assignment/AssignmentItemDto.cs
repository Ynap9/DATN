using System.ComponentModel.DataAnnotations;

namespace sh.be.applications.Module.Operation.Dtos.Assignment
{
    public class AssignmentItemDto
    {
        [Required(AllowEmptyStrings = true)]
        [MaxLength(100)]
        public string Box { get; set; } = String.Empty;
        [MaxLength(100)]
        public string? ProfileRange { get; set; }
    }
}
