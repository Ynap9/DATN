namespace sh.be.applications.Module.Operation.Dtos.Assignment
{
    public class RevokeAssignmentDto
    {
        public int? TicketId { get; set; }
        public string? AssigneeUserId { get; set; }
        public List<AssignmentItemDto>? Items { get; set; }
    }
}
