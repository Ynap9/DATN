namespace sh.be.applications.Base.Dtos.BackgroundJob
{
    public class BackgroundJobDetailDto
    {
        public string JobId { get; set; } = String.Empty;
        public int Status { get; set; }
        public string? ErrorMessage { get; set; }
        public IReadOnlyDictionary<string, string?> Parameters { get; set; } = new Dictionary<string, string?>();
    }
}
