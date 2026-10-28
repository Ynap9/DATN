namespace sh.be.applications.Module.Operation.Dtos.ProjectMember
{
    public class AddProjectMemberResultDto
    {
        public int AddedCount { get; set; }
        public List<ViewProjectMemberDto> Added { get; set; } = new();
        public List<SkippedMemberDto> Skipped { get; set; } = new();
    }
}
