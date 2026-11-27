using sh.be.applications.Module.Packaging.Dtos.PackageSession;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Packaging.Interfaces.Services
{
    public interface IPackageSessionService
    {
        Task<ViewCreatePackageSessionDto> Create(CreatePackageSessionDto dto);
        Task<ViewAddFilesPackageSessionDto> AddFiles(int id, AddFilesPackageSessionDto dto);
        Task<PackageSessionProgressDto> Start(int id);
        Task<PackageSessionProgressDto> GetProgress(int id);
        Task<ViewPackageSessionResultDto> GetResult(int id);
        Task<BaseResponsePagingDto<GetListPackageDocumentDto>> FindDocumentPaging(int id, FindPagingPackageDocumentDto dto);
        Task<PackageSessionProgressDto> Cancel(int id);
        Task VerifyFromJob(int id, string userId);
    }
}
