namespace sh.be.applications.Module.Construction.Dtos.ProjectConfig
{
    public class FindProjectConfigByIdDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public string Value { get; set; } = String.Empty;
        public string Description { get; set; } = String.Empty;
    }
}
