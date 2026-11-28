using System.Text.Json.Serialization;

namespace sh.be.applications.Module.Packaging.Dtos.PackageBuild
{
    public class GetListPackageBuildDto
    {
        public int Id { get; set; }
        public string? PackageCode { get; set; }
        public string? PackageObjid { get; set; }
        public string? PackageFileName { get; set; }
        public long? PackageSize { get; set; }
        [JsonIgnore]
        public string PackageObjectKey { get; set; } = String.Empty;
        public string DownloadUrl { get; set; } = String.Empty;
    }
}
