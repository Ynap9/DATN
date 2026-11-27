using Microsoft.AspNetCore.Http;

namespace sh.be.applications.Module.Packaging.Dtos.PackageSession
{
    public class AddFilesPackageSessionDto
    {
        public List<IFormFile> Files { get; set; } = new();
        public List<string> RelativePaths { get; set; } = new();
    }
}
