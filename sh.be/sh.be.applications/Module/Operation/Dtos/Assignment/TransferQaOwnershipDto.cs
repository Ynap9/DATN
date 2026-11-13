using System.ComponentModel.DataAnnotations;

namespace sh.be.applications.Module.Operation.Dtos.Assignment
{
    public class TransferQaOwnershipDto
    {
        [Required]
        public int TicketId { get; set; }
        [Required]
        public string ToUserId { get; set; } = String.Empty;
        public List<AssignmentItemDto>? Items { get; set; }
    }
}
