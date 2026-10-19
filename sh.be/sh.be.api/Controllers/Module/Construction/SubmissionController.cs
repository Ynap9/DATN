using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Construction.Dtos.Submission;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;
using System.Net.Mime;

namespace sh.be.api.Controllers.Module.Construction
{
    [Route("api/v{version:apiVersion}/construction/submission")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SubmissionController : BaseController
    {
        private const string FileCacheControl = "private, max-age=300";

        private readonly ISubmissionService _submissionService;
        private readonly ISubmissionFileService _submissionFileService;

        public SubmissionController(ILogger<SubmissionController> logger, ISubmissionService submissionService,
            ISubmissionFileService submissionFileService) : base(logger)
        {
            _submissionService = submissionService;
            _submissionFileService = submissionFileService;
        }

        [HttpPost("import/append/verify")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_IMPORT)]
        public async Task<ApiResponse> VerifyImportAppend([FromBody] ImportSubmissionDto dto)
        {
            try
            {
                var data = await _submissionService.VerifyImportAppend(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("import/append")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_IMPORT)]
        public async Task<ApiResponse> ImportAppend([FromBody] ImportSubmissionDto dto)
        {
            try
            {
                var data = await _submissionService.ImportAppend(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("import/blank/verify")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_IMPORT)]
        public async Task<ApiResponse> VerifyImportBlank([FromBody] ImportSubmissionDto dto)
        {
            try
            {
                var data = await _submissionService.VerifyImportBlank(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("import/blank")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_IMPORT)]
        public async Task<ApiResponse> ImportBlank([FromBody] ImportSubmissionDto dto)
        {
            try
            {
                var data = await _submissionService.ImportBlank(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("import/overwrite/verify")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_IMPORT)]
        public async Task<ApiResponse> VerifyImportOverwrite([FromBody] ImportOverwriteSubmissionDto dto)
        {
            try
            {
                var data = await _submissionService.VerifyImportOverwrite(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("import/overwrite")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_IMPORT)]
        public async Task<ApiResponse> ImportOverwrite([FromBody] ImportOverwriteSubmissionDto dto)
        {
            try
            {
                var data = await _submissionService.ImportOverwrite(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("paging")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_VIEW)]
        public async Task<ApiResponse> FindPaging([FromQuery] FindPagingSubmissionDto dto)
        {
            try
            {
                var data = await _submissionService.FindPaging(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("count")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_VIEW)]
        public async Task<ApiResponse> Count([FromQuery] SubmissionFilterDto dto)
        {
            try
            {
                var data = await _submissionService.Count(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id:int}/next")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_VIEW)]
        public async Task<ApiResponse> FindNext([FromRoute] int id, [FromQuery] SubmissionFilterDto dto)
        {
            try
            {
                var data = await _submissionService.FindNext(id, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id:int}/previous")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_VIEW)]
        public async Task<ApiResponse> FindPrevious([FromRoute] int id, [FromQuery] SubmissionFilterDto dto)
        {
            try
            {
                var data = await _submissionService.FindPrevious(id, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id:int}")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_VIEW)]
        public async Task<ApiResponse> FindById([FromRoute] int id, [FromQuery] FindSubmissionDetailDto dto)
        {
            try
            {
                var data = await _submissionService.FindById(id, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("{id:int}/check")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_CHECK)]
        public async Task<ApiResponse> Check([FromRoute] int id, [FromBody] CheckSubmissionDto dto)
        {
            try
            {
                var data = await _submissionService.Check(id, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id:int}/logs")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_VIEW)]
        public async Task<ApiResponse> FindLogPaging([FromRoute] int id, [FromQuery] FindPagingSubmissionLogDto dto)
        {
            try
            {
                var data = await _submissionService.FindLogPaging(id, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("proxy-file")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_VIEW)]
        public async Task<IActionResult> ProxyFile([FromQuery] string url)
        {
            try
            {
                var file = await _submissionFileService.OpenPdf(url, HttpContext.RequestAborted);
                var contentDisposition = new ContentDispositionHeaderValue(DispositionTypeNames.Inline);
                contentDisposition.SetHttpFileName(file.FileName);
                Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();
                Response.Headers[HeaderNames.CacheControl] = FileCacheControl;
                return File(file.Content, file.ContentType);
            }
            catch (Exception ex)
            {
                return Ok(OkException(ex));
            }
        }
    }
}
