using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.DigitalSignature.Dtos.Template;
using sh.be.applications.Module.DigitalSignature.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.DigitalSignature
{
    [Route("api/v{version:apiVersion}/digital-signature/template")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class TemplateController : BaseController
    {
        private readonly ITemplateService _templateService;
        public TemplateController(ILogger<TemplateController> logger, ITemplateService templateService) : base(logger)
        {
            _templateService = templateService;
        }

        [HttpGet("paging")]
        [Permission(PermissionConstants.FUNCTION_TEMPLATE_VIEW)]
        public async Task<ApiResponse> FindPaging([FromQuery] FindPagingTemplateDto dto)
        {
            try
            {
                var data = await _templateService.FindPaging(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("sample-file")]
        [Permission(PermissionConstants.FUNCTION_TEMPLATE_VIEW)]
        public IActionResult GetSampleFile()
        {
            try
            {
                var file = _templateService.GetSampleFile();
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                return Ok(OkException(ex));
            }
        }

        [HttpGet("{id}")]
        [Permission(PermissionConstants.FUNCTION_TEMPLATE_VIEW)]
        public async Task<ApiResponse> FindById([FromRoute] int id)
        {
            try
            {
                var data = await _templateService.FindById(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("")]
        [Permission(PermissionConstants.FUNCTION_TEMPLATE_CREATE)]
        public async Task<ApiResponse> Create([FromBody] CreateTemplateDto dto)
        {
            try
            {
                var data = await _templateService.Create(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("{id}")]
        [Permission(PermissionConstants.FUNCTION_TEMPLATE_UPDATE)]
        public async Task<ApiResponse> Update([FromRoute] int id, [FromBody] UpdateTemplateDto dto)
        {
            try
            {
                await _templateService.Update(id, dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("{id}/config")]
        [Permission(PermissionConstants.FUNCTION_TEMPLATE_UPDATE)]
        public async Task<ApiResponse> UpdateConfig([FromRoute] int id, [FromForm] UpdateConfigTemplateDto dto)
        {
            try
            {
                var data = await _templateService.UpdateConfig(id, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpDelete("{id}")]
        [Permission(PermissionConstants.FUNCTION_TEMPLATE_DELETE)]
        public async Task<ApiResponse> Delete([FromRoute] int id)
        {
            try
            {
                await _templateService.Delete(id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
