using System.ComponentModel.DataAnnotations;

namespace sh.be.applications.Module.Construction.Dtos.QaAssignment
{
    public class QaTicketItemDto
    {
        [Required]
        public string Box { get; set; } = String.Empty;
        public string? ProfileRange { get; set; }
    }
}
