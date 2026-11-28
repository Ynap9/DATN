using sh.be.applications.Module.Packaging.Dtos.PackageBuild;

namespace sh.be.applications.Module.Packaging.Interfaces.Services
{
    public interface IPackageBuildService
    {
        Task<PackageBuildProgressDto> Build(int id, BuildPackageSessionDto dto);
        Task<PackageBuildProgressDto> GetBuildProgress(int id);
        Task<List<GetListPackageBuildDto>> GetPackages(int id);
        Task BuildFromJob(int id, string userId);
    }
}
