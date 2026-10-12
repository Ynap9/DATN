using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Construction.Dtos.DocumentType;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Construction
{
    [Route("api/construction/document-type")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class DocumentTypeController : BaseController
    {
        private readonly IDocumentTypeService _documentTypeService;
        public DocumentTypeController(ILogger<DocumentTypeController> logger, IDocumentTypeService documentTypeService) : base(logger)
        {
            _documentTypeService = documentTypeService;
        }

        [HttpGet("dropdown")]
        [Permission(PermissionConstants.FUNCTION_DOCUMENT_TYPE_VIEW)]
        public async Task<ApiResponse> GetDocumentTypeDropDown()
        {
            try
            {
                var data = await _documentTypeService.GetDocumentTypeDropDown();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("paging")]
        [Permission(PermissionConstants.FUNCTION_DOCUMENT_TYPE_VIEW)]
        public async Task<ApiResponse> FindPaging([FromQuery] FindPagingDocumentTypeDto dto)
        {
            try
            {
                var data = await _documentTypeService.FindPaging(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}")]
        [Permission(PermissionConstants.FUNCTION_DOCUMENT_TYPE_VIEW)]
        public async Task<ApiResponse> FindById([FromRoute] int id)
        {
            try
            {
                var data = await _documentTypeService.FindById(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("")]
        [Permission(PermissionConstants.FUNCTION_DOCUMENT_TYPE_CREATE)]
        public async Task<ApiResponse> Create([FromBody] CreateDocumentTypeDto dto)
        {
            try
            {
                await _documentTypeService.Create(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("{id}")]
        [Permission(PermissionConstants.FUNCTION_DOCUMENT_TYPE_UPDATE)]
        public async Task<ApiResponse> Update([FromRoute] int id, [FromBody] UpdateDocumentTypeDto dto)
        {
            try
            {
                await _documentTypeService.Update(id, dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpDelete("{id}")]
        [Permission(PermissionConstants.FUNCTION_DOCUMENT_TYPE_DELETE)]
        public async Task<ApiResponse> Delete([FromRoute] int id)
        {
            try
            {
                await _documentTypeService.Delete(id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
