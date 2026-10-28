namespace sh.be.applications.Module.Operation.Dtos.ProjectMember
{
    public class RemoveProjectMemberResultDto
    {
        public int RevokedDocumentCount { get; set; }
        public int KeptDocumentCount { get; set; }
        public List<int> CancelledAssignmentTicketIds { get; set; } = new();
        public List<int> CancelledQaTicketIds { get; set; } = new();
        public int UnmanagedMemberCount { get; set; }
    }
}
