using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Packaging.Dtos.PackageConfig;
using sh.be.applications.Module.Packaging.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Packaging
{
    [Route("api/v{version:apiVersion}/packaging/config")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PackageConfigController : BaseController
    {
        private readonly IPackageConfigService _packageConfigService;
        public PackageConfigController(ILogger<PackageConfigController> logger, IPackageConfigService packageConfigService) : base(logger)
        {
            _packageConfigService = packageConfigService;
        }

        [HttpGet("dropdown")]
        [Permission(PermissionConstants.FUNCTION_PACKAGE_CONFIG_VIEW)]
        public async Task<ApiResponse> GetPackageTypeDropDown()
        {
            try
            {
                var data = await _packageConfigService.GetPackageTypeDropDown();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("paging")]
        [Permission(PermissionConstants.FUNCTION_PACKAGE_CONFIG_VIEW)]
        public async Task<ApiResponse> FindFieldPaging([FromQuery] FindPagingPackageSchemaFieldDto dto)
        {
            try
            {
                var data = await _packageConfigService.FindFieldPaging(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("excel-template")]
        [Permission(PermissionConstants.FUNCTION_PACKAGE_CONFIG_VIEW)]
        public async Task<IActionResult> ExportExcelTemplate([FromQuery] ExportPackageExcelTemplateDto dto)
        {
            try
            {
                var file = await _packageConfigService.ExportExcelTemplate(dto);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                return Ok(OkException(ex));
            }
        }
    }
}
