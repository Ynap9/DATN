namespace sh.be.applications.Module.Ai.Dtos.AiProvider
{
    public class GetListAiProviderDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = String.Empty;
        public string Name { get; set; } = String.Empty;
        public string Protocol { get; set; } = String.Empty;
        public string BaseUrl { get; set; } = String.Empty;
        public string Scope { get; set; } = String.Empty;
        public bool HasApiKey { get; set; }
        public string? ApiKeyFingerprint { get; set; }
        public bool IsActive { get; set; }
        public int ModelCount { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
