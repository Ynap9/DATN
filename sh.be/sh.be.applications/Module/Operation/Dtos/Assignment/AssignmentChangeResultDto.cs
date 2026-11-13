namespace sh.be.applications.Module.Operation.Dtos.Assignment
{
    public class AssignmentChangeResultDto
    {
        public int AffectedDocuments { get; set; }
        public List<int> CancelledTicketIds { get; set; } = new();
        public List<int> DoneTicketIds { get; set; } = new();
        public ViewAssignmentTicketDto? NewTicket { get; set; }
    }
}
