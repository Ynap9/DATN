using sh.be.applications.Base.Dtos.DataReader;

namespace sh.be.applications.Base.Interfaces.External.DataReader
{
    public interface IPackageFileInspectExternal
    {
        Task<PackageFileInspectDto> InspectAsync(Stream content, bool isPdf);
    }
}
