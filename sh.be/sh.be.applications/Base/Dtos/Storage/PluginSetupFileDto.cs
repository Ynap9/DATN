namespace sh.be.applications.Base.Dtos.Storage
{
    public class PluginSetupFileDto
    {
        public string FileName { get; set; } = String.Empty;
        public string ContentType { get; set; } = String.Empty;
        public Stream Content { get; set; } = Stream.Null;
    }
}
