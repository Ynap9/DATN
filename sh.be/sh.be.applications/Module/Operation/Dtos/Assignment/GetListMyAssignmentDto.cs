namespace sh.be.applications.Module.Operation.Dtos.Assignment
{
    public class GetListMyAssignmentDto
    {
        public string Kind { get; set; } = String.Empty;
        public int TicketId { get; set; }
        public int ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public int DocumentTypeId { get; set; }
        public string? DocumentTypeName { get; set; }
        public string Status { get; set; } = String.Empty;
        public int DocumentCount { get; set; }
        public int Total { get; set; }
        public int Checked { get; set; }
        public int Returned { get; set; }
        public int QaPassed { get; set; }
        public DateTime? CreatedDate { get; set; }
        public List<AssignmentItemDto> Items { get; set; } = new();
    }
}
