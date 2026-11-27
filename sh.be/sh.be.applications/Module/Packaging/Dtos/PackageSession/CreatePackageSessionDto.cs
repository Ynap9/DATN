namespace sh.be.applications.Module.Packaging.Dtos.PackageSession
{
    public class CreatePackageSessionDto
    {
        public int PackageType { get; set; }
        public List<int> MetadataTypes { get; set; } = new();
    }
}
