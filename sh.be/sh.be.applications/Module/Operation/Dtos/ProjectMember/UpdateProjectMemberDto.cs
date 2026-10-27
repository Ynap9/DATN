namespace sh.be.applications.Module.Operation.Dtos.ProjectMember
{
    public class UpdateProjectMemberDto
    {
        public string? ManagedByUserId { get; set; }
        public bool ClearManager { get; set; }
        public bool? IsActive { get; set; }
    }
}
