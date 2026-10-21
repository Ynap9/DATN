namespace sh.be.applications.Module.DigitalSignature.Dtos.Plugin
{
    public class PluginVersionDto
    {
        public string Version { get; set; } = String.Empty;
        public bool IsSupported { get; set; }
        public IReadOnlyList<string> SupportedVersions { get; set; } = [];
        public string? Reason { get; set; }
    }
}
