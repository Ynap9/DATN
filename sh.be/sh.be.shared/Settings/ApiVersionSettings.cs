namespace sh.be.shared.Settings
{
    public class ApiVersionSettings
    {
        public List<ApiVersionSunsetSettings> Sunsets { get; set; } = new();
    }

    public class ApiVersionSunsetSettings
    {
        public string Version { get; set; } = String.Empty;
        public DateTimeOffset Date { get; set; }
        public string? Link { get; set; }
    }
}
