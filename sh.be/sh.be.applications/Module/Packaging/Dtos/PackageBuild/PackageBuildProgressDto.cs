namespace sh.be.applications.Module.Packaging.Dtos.PackageBuild
{
    public class PackageBuildProgressDto
    {
        public int Id { get; set; }
        public string? BuildStatus { get; set; }
        public int BuildTotal { get; set; }
        public int BuildDone { get; set; }
        public string? BuildFailReason { get; set; }
    }
}
