namespace sh.be.applications.Module.Construction.Dtos.Project
{
    public class GetListProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int Status { get; set; }
        public string UrlGoogleSheetSync { get; set; } = String.Empty;
    }
}
