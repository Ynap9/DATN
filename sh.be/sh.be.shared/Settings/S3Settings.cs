using Microsoft.Extensions.Configuration;

namespace sh.be.shared.Settings
{
    public class S3Settings
    {
        [ConfigurationKeyName("S3_URL")]
        public string Url { get; set; } = String.Empty;
        [ConfigurationKeyName("S3_REGION")]
        public string Region { get; set; } = String.Empty;
        [ConfigurationKeyName("S3_BUCKET")]
        public string Bucket { get; set; } = String.Empty;
        [ConfigurationKeyName("S3_ACCESS_KEY")]
        public string AccessKey { get; set; } = String.Empty;
        [ConfigurationKeyName("S3_SECRET_KEY")]
        public string SecretKey { get; set; } = String.Empty;
    }
}
