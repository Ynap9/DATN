using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Packaging.Dtos.PackageBuild;
using sh.be.applications.Module.Packaging.Dtos.PackageSession;
using sh.be.applications.Module.Packaging.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Packaging;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Packaging
{
    [Route("api/v{version:apiVersion}/packaging/session")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PackageSessionController : BaseController
    {
        private readonly IPackageSessionService _packageSessionService;
        private readonly IPackageBuildService _packageBuildService;
        public PackageSessionController(ILogger<PackageSessionController> logger, IPackageSessionService packageSessionService, IPackageBuildService packageBuildService) : base(logger)
        {
            _packageSessionService = packageSessionService;
            _packageBuildService = packageBuildService;
        }

        [HttpPost]
        [Permission(PermissionConstants.FUNCTION_PACKAGE_BUILD)]
        public async Task<ApiResponse> Create([FromBody] CreatePackageSessionDto dto)
        {
            try
            {
                var data = await _packageSessionService.Create(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("{id}/files")]
        [Permission(PermissionConstants.FUNCTION_PACKAGE_BUILD)]
        [RequestSizeLimit(PackageSessionConst.MAX_UPLOAD_BYTES)]
        [RequestFormLimits(MultipartBodyLengthLimit = PackageSessionConst.MAX_UPLOAD_BYTES)]
        public async Task<ApiResponse> AddFiles([FromRoute] int id, [FromForm] AddFilesPackageSessionDto dto)
        {
            try
            {
                var data = await _packageSessionService.AddFiles(id, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("{id}/start")]
        [Permission(PermissionConstants.FUNCTION_PACKAGE_BUILD)]
        public async Task<ApiResponse> Start([FromRoute] int id)
        {
            try
            {
                var data = await _packageSessionService.Start(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}/progress")]
        [Permission(PermissionConstants.FUNCTION_PACKAGE_BUILD)]
        public async Task<ApiResponse> GetProgress([FromRoute] int id)
        {
            try
            {
                var data = await _packageSessionService.GetProgress(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}/result")]
        [Permission(PermissionConstants.FUNCTION_PACKAGE_BUILD)]
        public async Task<ApiResponse> GetResult([FromRoute] int id)
        {
            try
            {
                var data = await _packageSessionService.GetResult(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}/documents")]
        [Permission(PermissionConstants.FUNCTION_PACKAGE_BUILD)]
        public async Task<ApiResponse> FindDocumentPaging([FromRoute] int id, [FromQuery] FindPagingPackageDocumentDto dto)
        {
            try
            {
                var data = await _packageSessionService.FindDocumentPaging(id, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("{id}/cancel")]
        [Permission(PermissionConstants.FUNCTION_PACKAGE_BUILD)]
        public async Task<ApiResponse> Cancel([FromRoute] int id)
        {
            try
            {
                var data = await _packageSessionService.Cancel(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("{id}/build")]
        [Permission(PermissionConstants.FUNCTION_PACKAGE_BUILD)]
        public async Task<ApiResponse> Build([FromRoute] int id, [FromBody] BuildPackageSessionDto dto)
        {
            try
            {
                var data = await _packageBuildService.Build(id, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}/build-progress")]
        [Permission(PermissionConstants.FUNCTION_PACKAGE_BUILD)]
        public async Task<ApiResponse> GetBuildProgress([FromRoute] int id)
        {
            try
            {
                var data = await _packageBuildService.GetBuildProgress(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}/packages")]
        [Permission(PermissionConstants.FUNCTION_PACKAGE_BUILD)]
        public async Task<ApiResponse> GetPackages([FromRoute] int id)
        {
            try
            {
                var data = await _packageBuildService.GetPackages(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
