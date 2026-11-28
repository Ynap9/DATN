using sh.be.applications.Base.Dtos.Storage;

namespace sh.be.applications.Base.Interfaces.External.Storage
{
    public interface IPackageArchiveExternal
    {
        Task<long> BuildAsync(PackageArchiveDto dto, string objectKey);
    }
}
