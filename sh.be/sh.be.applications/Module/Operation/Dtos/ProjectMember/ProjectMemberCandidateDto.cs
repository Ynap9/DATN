namespace sh.be.applications.Module.Operation.Dtos.ProjectMember
{
    public class ProjectMemberCandidateDto
    {
        public string UserId { get; set; } = String.Empty;
        public string UserName { get; set; } = String.Empty;
        public string? FullName { get; set; }
        public string Email { get; set; } = String.Empty;
    }
}
