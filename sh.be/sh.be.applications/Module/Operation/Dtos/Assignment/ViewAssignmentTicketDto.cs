namespace sh.be.applications.Module.Operation.Dtos.Assignment
{
    public class ViewAssignmentTicketDto
    {
        public int Id { get; set; }
        public string Role { get; set; } = String.Empty;
        public int ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public int DocumentTypeId { get; set; }
        public string? DocumentTypeName { get; set; }
        public string AssigneeUserId { get; set; } = String.Empty;
        public string? AssigneeFullName { get; set; }
        public int Status { get; set; }
        public int DocumentCount { get; set; }
        public string? CreatedBy { get; set; }
        public string? CreatedByFullName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? DoneAt { get; set; }
        public List<AssignmentItemDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Checked { get; set; }
        public int Returned { get; set; }
        public int QaPassed { get; set; }
    }
}
