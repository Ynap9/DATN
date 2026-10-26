using System.Text.Json.Serialization;

namespace sh.be.applications.Module.Ai.Dtos.AiProvider
{
    public class AiProviderApiKeyFileDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = String.Empty;
        [JsonPropertyName("provider_code")]
        public string ProviderCode { get; set; } = String.Empty;
        [JsonPropertyName("provider_name")]
        public string ProviderName { get; set; } = String.Empty;
        [JsonPropertyName("protocol")]
        public string Protocol { get; set; } = String.Empty;
        [JsonPropertyName("base_url")]
        public string BaseUrl { get; set; } = String.Empty;
        [JsonPropertyName("api_key_header_name")]
        public string? ApiKeyHeaderName { get; set; }
        [JsonPropertyName("api_key_header_prefix")]
        public string? ApiKeyHeaderPrefix { get; set; }
        [JsonPropertyName("api_key")]
        public string ApiKey { get; set; } = String.Empty;
        [JsonPropertyName("api_key_fingerprint")]
        public string? ApiKeyFingerprint { get; set; }
        [JsonPropertyName("exported_by")]
        public string? ExportedBy { get; set; }
        [JsonPropertyName("exported_at")]
        public DateTimeOffset ExportedAt { get; set; }
    }
}
