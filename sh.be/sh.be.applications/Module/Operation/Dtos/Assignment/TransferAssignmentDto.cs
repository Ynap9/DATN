using System.ComponentModel.DataAnnotations;

namespace sh.be.applications.Module.Operation.Dtos.Assignment
{
    public class TransferAssignmentDto
    {
        [Required]
        public string FromUserId { get; set; } = String.Empty;
        [Required]
        public string ToUserId { get; set; } = String.Empty;
        public int? TicketId { get; set; }
        public List<AssignmentItemDto>? Items { get; set; }
    }
}
