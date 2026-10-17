namespace sh.be.applications.Module.Construction.Dtos.ProjectConfig
{
    public class CreateProjectConfigDto
    {
        public string Name { get; set; } = String.Empty;
        public string Value { get; set; } = String.Empty;
        public string? Description { get; set; }
    }
}
