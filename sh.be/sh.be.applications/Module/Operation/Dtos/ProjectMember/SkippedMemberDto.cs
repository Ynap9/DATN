namespace sh.be.applications.Module.Operation.Dtos.ProjectMember
{
    public class SkippedMemberDto
    {
        public string UserId { get; set; } = String.Empty;
        public string? FullName { get; set; }
        public string Reason { get; set; } = String.Empty;
    }
}
