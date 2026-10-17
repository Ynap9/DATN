namespace sh.be.shared.Constants.DigitalSignature
{
    public static class PluginConst
    {
        public const string CONFIG_SECTION = "Plugin";
        public const string SETUP_FOLDER = "Plugins";
        public const string SETUP_FILE_NAME = "Ký số plugin.exe";
        public const string SETUP_CONTENT_TYPE = "application/octet-stream";

        public static readonly IReadOnlyList<string> DEFAULT_SUPPORTED_VERSIONS = ["3.0.0"];

        public const string VERSION_MISSING_REASON = "Chưa đọc được phiên bản plugin. Cài lại bộ cài mới nhất rồi thử lại.";
        public const string VERSION_UNSUPPORTED_REASON = "Plugin phiên bản {0} không còn phù hợp. Tải và cài lại bộ cài mới nhất.";
    }
}
