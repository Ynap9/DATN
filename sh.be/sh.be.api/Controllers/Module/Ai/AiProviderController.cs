using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Ai.Dtos.AiProvider;
using sh.be.applications.Module.Ai.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Ai
{
    [Route("api/v{version:apiVersion}/ai/provider")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AiProviderController : BaseController
    {
        private const string NoStoreCacheControl = "no-store";

        private readonly IAiProviderService _aiProviderService;
        public AiProviderController(ILogger<AiProviderController> logger, IAiProviderService aiProviderService) : base(logger)
        {
            _aiProviderService = aiProviderService;
        }

        [HttpGet("paging")]
        [Permission(PermissionConstants.FUNCTION_AI_PROVIDER_VIEW)]
        public async Task<ApiResponse> FindPaging([FromQuery] FindPagingAiProviderDto dto)
        {
            try
            {
                var data = await _aiProviderService.FindPaging(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("dropdown")]
        [Permission(PermissionConstants.FUNCTION_AI_PROVIDER_VIEW)]
        public async Task<ApiResponse> GetDropDown()
        {
            try
            {
                var data = await _aiProviderService.GetDropDown();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id:int}")]
        [Permission(PermissionConstants.FUNCTION_AI_PROVIDER_VIEW)]
        public async Task<ApiResponse> FindById([FromRoute] int id)
        {
            try
            {
                var data = await _aiProviderService.FindById(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("")]
        [Permission(PermissionConstants.FUNCTION_AI_PROVIDER_CREATE)]
        public async Task<ApiResponse> Create([FromBody] CreateAiProviderDto dto)
        {
            try
            {
                var data = await _aiProviderService.Create(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("{id:int}")]
        [Permission(PermissionConstants.FUNCTION_AI_PROVIDER_UPDATE)]
        public async Task<ApiResponse> Update([FromRoute] int id, [FromBody] UpdateAiProviderDto dto)
        {
            try
            {
                await _aiProviderService.Update(id, dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("{id:int}/api-key")]
        [Permission(PermissionConstants.FUNCTION_AI_PROVIDER_UPDATE)]
        public async Task<ApiResponse> UpdateApiKey([FromRoute] int id, [FromBody] UpdateAiProviderApiKeyDto dto)
        {
            try
            {
                var data = await _aiProviderService.UpdateApiKey(id, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpDelete("{id:int}")]
        [Permission(PermissionConstants.FUNCTION_AI_PROVIDER_DELETE)]
        public async Task<ApiResponse> Delete([FromRoute] int id)
        {
            try
            {
                await _aiProviderService.Delete(id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id:int}/api-key/download")]
        [Permission(PermissionConstants.FUNCTION_AI_PROVIDER_DOWNLOAD_API_KEY)]
        public async Task<IActionResult> DownloadApiKey([FromRoute] int id)
        {
            try
            {
                var file = await _aiProviderService.DownloadApiKey(id);
                Response.Headers.CacheControl = NoStoreCacheControl;
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                return Ok(OkException(ex));
            }
        }
    }
}
