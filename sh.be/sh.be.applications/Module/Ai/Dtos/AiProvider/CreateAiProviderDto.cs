namespace sh.be.applications.Module.Ai.Dtos.AiProvider
{
    public class CreateAiProviderDto
    {
        public string Code { get; set; } = String.Empty;
        public string Name { get; set; } = String.Empty;
        public string Protocol { get; set; } = String.Empty;
        public string BaseUrl { get; set; } = String.Empty;
        public string Scope { get; set; } = String.Empty;
        public string? ApiKeyHeaderName { get; set; }
        public string? ApiKeyHeaderPrefix { get; set; }
        public int? TimeoutSeconds { get; set; }
        public int? MaxConcurrency { get; set; }
        public int? RateLimitPerMinute { get; set; }
        public Dictionary<string, string>? ExtraHeaders { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
