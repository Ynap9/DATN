namespace sh.be.applications.Module.Operation.Dtos.Assignment
{
    public class AssignmentConflictDto
    {
        public string Box { get; set; } = String.Empty;
        public string ProfileRange { get; set; } = String.Empty;
        public string HolderUserId { get; set; } = String.Empty;
        public string? HolderFullName { get; set; }
        public int DocumentCount { get; set; }
        public bool IsOwner { get; set; }
    }
}
