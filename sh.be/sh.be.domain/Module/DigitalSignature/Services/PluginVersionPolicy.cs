using sh.be.shared.Constants.DigitalSignature;

namespace sh.be.domain.Module.DigitalSignature.Services
{
    public static class PluginVersionPolicy
    {
        public static IReadOnlyList<string> ResolveSupportedVersions(IReadOnlyList<string> configuredVersions)
        {
            return configuredVersions.Count > 0 ? configuredVersions : PluginConst.DEFAULT_SUPPORTED_VERSIONS;
        }

        public static string? ResolveUnsupportedReason(string version, IReadOnlyList<string> supportedVersions)
        {
            if (version.Length == 0)
            {
                return PluginConst.VERSION_MISSING_REASON;
            }
            return supportedVersions.Any(v => string.Equals(v.Trim(), version, StringComparison.OrdinalIgnoreCase))
                ? null
                : string.Format(PluginConst.VERSION_UNSUPPORTED_REASON, version);
        }
    }
}
