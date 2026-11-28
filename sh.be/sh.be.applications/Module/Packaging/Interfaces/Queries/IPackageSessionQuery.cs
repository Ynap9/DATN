using sh.be.applications.Module.Packaging.Dtos.PackageBuild;
using sh.be.applications.Module.Packaging.Dtos.PackageSession;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Packaging.Interfaces.Queries
{
    public interface IPackageSessionQuery
    {
        Task<PackageSessionProgressDto?> FindProgress(int id);
        Task<ViewPackageSessionResultDto?> FindResult(int id);
        Task<string?> FindMatchReport(int id);
        Task<BaseResponsePagingDto<GetListPackageDocumentDto>> FindDocumentPaging(int sessionId, FindPagingPackageDocumentDto dto);
        Task<PackageBuildProgressDto?> FindBuildProgress(int id);
        Task<List<GetListPackageBuildDto>> GetPackages(int sessionId);
    }
}
