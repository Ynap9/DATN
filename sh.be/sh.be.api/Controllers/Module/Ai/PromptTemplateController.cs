using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Ai.Dtos.PromptTemplate;
using sh.be.applications.Module.Ai.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Ai
{
    [Route("api/v{version:apiVersion}/ai/prompt-template")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PromptTemplateController : BaseController
    {
        private readonly IPromptTemplateService _promptTemplateService;
        public PromptTemplateController(ILogger<PromptTemplateController> logger, IPromptTemplateService promptTemplateService) : base(logger)
        {
            _promptTemplateService = promptTemplateService;
        }

        [HttpGet("document-type/{documentTypeId:int}")]
        [Permission(PermissionConstants.FUNCTION_AI_PROMPT_TEMPLATE_VIEW)]
        public async Task<ApiResponse> FindEditor([FromRoute] int documentTypeId)
        {
            try
            {
                var data = await _promptTemplateService.FindEditor(documentTypeId);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("document-type/{documentTypeId:int}/versions")]
        [Permission(PermissionConstants.FUNCTION_AI_PROMPT_TEMPLATE_VIEW)]
        public async Task<ApiResponse> FindVersions([FromRoute] int documentTypeId)
        {
            try
            {
                var data = await _promptTemplateService.FindVersions(documentTypeId);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("document-type/{documentTypeId:int}/draft")]
        [Permission(PermissionConstants.FUNCTION_AI_PROMPT_TEMPLATE_UPDATE)]
        public async Task<ApiResponse> SaveDraft([FromRoute] int documentTypeId, [FromBody] SavePromptDraftDto dto)
        {
            try
            {
                var data = await _promptTemplateService.SaveDraft(documentTypeId, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("{id:int}/publish")]
        [Permission(PermissionConstants.FUNCTION_AI_PROMPT_TEMPLATE_PUBLISH)]
        public async Task<ApiResponse> Publish([FromRoute] int id, [FromBody] PublishPromptTemplateDto dto)
        {
            try
            {
                await _promptTemplateService.Publish(id, dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("{id:int}/restore")]
        [Permission(PermissionConstants.FUNCTION_AI_PROMPT_TEMPLATE_UPDATE)]
        public async Task<ApiResponse> Restore([FromRoute] int id)
        {
            try
            {
                var data = await _promptTemplateService.Restore(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpDelete("{id:int}")]
        [Permission(PermissionConstants.FUNCTION_AI_PROMPT_TEMPLATE_UPDATE)]
        public async Task<ApiResponse> Discard([FromRoute] int id)
        {
            try
            {
                await _promptTemplateService.Discard(id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id:int}/preview")]
        [Permission(PermissionConstants.FUNCTION_AI_PROMPT_TEMPLATE_VIEW)]
        public async Task<ApiResponse> Preview([FromRoute] int id)
        {
            try
            {
                var data = await _promptTemplateService.Preview(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
