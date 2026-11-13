using System.ComponentModel.DataAnnotations;

namespace sh.be.applications.Module.Operation.Dtos.Assignment
{
    public class RevokeQaOwnershipDto
    {
        [Required]
        public int TicketId { get; set; }
        public List<AssignmentItemDto>? Items { get; set; }
    }
}
