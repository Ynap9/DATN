namespace sh.be.applications.Module.Operation.Dtos.ProjectMember
{
    public class UpdateMemberPricesResultDto
    {
        public int UpdatedCount { get; set; }
        public List<ViewProjectMemberDto> Members { get; set; } = [];
    }
}
