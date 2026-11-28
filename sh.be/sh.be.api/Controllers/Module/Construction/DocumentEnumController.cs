using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Construction.Dtos.DocumentEnum;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Construction
{
    [Route("api/v{version:apiVersion}/construction/document-type/{id}/enum")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class DocumentEnumController : BaseController
    {
        private readonly IDocumentEnumService _documentEnumService;
        public DocumentEnumController(ILogger<DocumentEnumController> logger, IDocumentEnumService documentEnumService) : base(logger)
        {
            _documentEnumService = documentEnumService;
        }

        [HttpGet("")]
        [Permission(PermissionConstants.FUNCTION_DOCUMENT_TYPE_VIEW)]
        public async Task<ApiResponse> GetList([FromRoute] int id)
        {
            try
            {
                var data = await _documentEnumService.GetList(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("")]
        [Permission(PermissionConstants.FUNCTION_DOCUMENT_TYPE_UPDATE)]
        public async Task<ApiResponse> Create([FromRoute] int id, [FromBody] CreateDocumentEnumDto dto)
        {
            try
            {
                var data = await _documentEnumService.Create(id, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("{enumId}")]
        [Permission(PermissionConstants.FUNCTION_DOCUMENT_TYPE_UPDATE)]
        public async Task<ApiResponse> Update([FromRoute] int id, [FromRoute] int enumId, [FromBody] UpdateDocumentEnumDto dto)
        {
            try
            {
                await _documentEnumService.Update(id, enumId, dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpDelete("{enumId}")]
        [Permission(PermissionConstants.FUNCTION_DOCUMENT_TYPE_UPDATE)]
        public async Task<ApiResponse> Delete([FromRoute] int id, [FromRoute] int enumId)
        {
            try
            {
                await _documentEnumService.Delete(id, enumId);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
