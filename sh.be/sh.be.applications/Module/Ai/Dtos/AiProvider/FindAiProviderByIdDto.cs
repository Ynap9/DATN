using System.Text.Json.Serialization;

namespace sh.be.applications.Module.Ai.Dtos.AiProvider
{
    public class FindAiProviderByIdDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = String.Empty;
        public string Name { get; set; } = String.Empty;
        public string Protocol { get; set; } = String.Empty;
        public string BaseUrl { get; set; } = String.Empty;
        public string Scope { get; set; } = String.Empty;
        public bool HasApiKey { get; set; }
        public string? ApiKeyFingerprint { get; set; }
        public string? ApiKeyHeaderName { get; set; }
        public string? ApiKeyHeaderPrefix { get; set; }
        public int TimeoutSeconds { get; set; }
        public int MaxConcurrency { get; set; }
        public int? RateLimitPerMinute { get; set; }
        public Dictionary<string, string> ExtraHeaders { get; set; } = new();
        [JsonIgnore]
        public string? ExtraHeadersJson { get; set; }
        public bool IsActive { get; set; }
        public int ModelCount { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
