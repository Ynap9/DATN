namespace sh.be.applications.Module.Operation.Dtos.Assignment
{
    public class AssignmentNodeDto
    {
        public string Box { get; set; } = String.Empty;
        public string? ProfileRange { get; set; }
        public int Total { get; set; }
        public int Assigned { get; set; }
        public int Unassigned { get; set; }
        public int Checked { get; set; }
        public int Returned { get; set; }
        public int QaPassed { get; set; }
        public int NotChecked { get; set; }
        public string? OwnerUserId { get; set; }
        public string? OwnerFullName { get; set; }
        public int HolderCount { get; set; }
        public List<AssignmentHolderDto> Holders { get; set; } = new();
    }
}
