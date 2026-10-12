using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Construction.Dtos.DocumentField;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Construction
{
    [Route("api/construction/document-field")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class DocumentFieldController : BaseController
    {
        private readonly IDocumentFieldService _documentFieldService;
        public DocumentFieldController(ILogger<DocumentFieldController> logger, IDocumentFieldService documentFieldService) : base(logger)
        {
            _documentFieldService = documentFieldService;
        }

        [HttpGet("dropdown/{documentTypeId}")]
        [Permission(PermissionConstants.FUNCTION_DOCUMENT_FIELD_VIEW)]
        public async Task<ApiResponse> GetDocumentFieldDropDown([FromRoute] int documentTypeId)
        {
            try
            {
                var data = await _documentFieldService.GetDocumentFieldDropDown(documentTypeId);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("paging")]
        [Permission(PermissionConstants.FUNCTION_DOCUMENT_FIELD_VIEW)]
        public async Task<ApiResponse> FindPaging([FromQuery] FindPagingDocumentFieldDto dto)
        {
            try
            {
                var data = await _documentFieldService.FindPaging(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}")]
        [Permission(PermissionConstants.FUNCTION_DOCUMENT_FIELD_VIEW)]
        public async Task<ApiResponse> FindById([FromRoute] int id)
        {
            try
            {
                var data = await _documentFieldService.FindById(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("")]
        [Permission(PermissionConstants.FUNCTION_DOCUMENT_FIELD_CREATE)]
        public async Task<ApiResponse> Create([FromBody] CreateDocumentFieldDto dto)
        {
            try
            {
                await _documentFieldService.Create(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("quick-create")]
        [Permission(PermissionConstants.FUNCTION_DOCUMENT_FIELD_CREATE)]
        public async Task<ApiResponse> QuickCreate([FromBody] QuickCreateDocumentFieldDto dto)
        {
            try
            {
                await _documentFieldService.QuickCreate(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("{id}")]
        [Permission(PermissionConstants.FUNCTION_DOCUMENT_FIELD_UPDATE)]
        public async Task<ApiResponse> Update([FromRoute] int id, [FromBody] UpdateDocumentFieldDto dto)
        {
            try
            {
                await _documentFieldService.Update(id, dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpDelete("quick-delete")]
        [Permission(PermissionConstants.FUNCTION_DOCUMENT_FIELD_DELETE)]
        public async Task<ApiResponse> QuickDelete([FromBody] QuickDeleteDocumentFieldDto dto)
        {
            try
            {
                await _documentFieldService.QuickDelete(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpDelete("{id}")]
        [Permission(PermissionConstants.FUNCTION_DOCUMENT_FIELD_DELETE)]
        public async Task<ApiResponse> Delete([FromRoute] int id)
        {
            try
            {
                await _documentFieldService.Delete(id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
