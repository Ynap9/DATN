using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Operation.Dtos.Wage;
using sh.be.applications.Module.Operation.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Operation
{
    [Route("api/v{version:apiVersion}/operation/project/{projectId:int}")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class WageController : BaseController
    {
        private readonly IWageService _wageService;
        public WageController(ILogger<WageController> logger, IWageService wageService) : base(logger)
        {
            _wageService = wageService;
        }

        [HttpGet("wage/paging")]
        [Permission(PermissionConstants.FUNCTION_WAGE_VIEW)]
        public async Task<ApiResponse> FindPaging([FromRoute] int projectId, [FromQuery] FindPagingWageDto dto)
        {
            try
            {
                var data = await _wageService.FindPaging(projectId, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("wage/export-excel")]
        [Permission(PermissionConstants.FUNCTION_WAGE_VIEW)]
        public async Task<IActionResult> ExportExcel([FromRoute] int projectId, [FromQuery] ExportWageDto dto)
        {
            try
            {
                var file = await _wageService.ExportExcel(projectId, dto);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                return Ok(OkException(ex));
            }
        }
    }
}
