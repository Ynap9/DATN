using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Construction.Dtos.Submission;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.HttpRequest;
using System.Net.Mime;

namespace sh.be.api.Controllers.Module.Construction
{
    [Route("api/construction/submission")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SubmissionController : BaseController
    {
        private const string FileCacheControl = "private, max-age=300";

        private readonly ISubmissionImportService _submissionImportService;
        private readonly ISubmissionFileService _submissionFileService;
        private readonly ISubmissionService _submissionService;

        public SubmissionController(ILogger<SubmissionController> logger, ISubmissionImportService submissionImportService,
            ISubmissionFileService submissionFileService, ISubmissionService submissionService) : base(logger)
        {
            _submissionImportService = submissionImportService;
            _submissionFileService = submissionFileService;
            _submissionService = submissionService;
        }

        [HttpPost("import/append/verify")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_IMPORT)]
        public async Task<ApiResponse> VerifyImportAppend([FromBody] ImportSubmissionDto dto)
        {
            try
            {
                var data = await _submissionImportService.VerifyImportAppend(dto);
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
                var data = await _submissionImportService.ImportAppend(dto);
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
                var data = await _submissionImportService.VerifyImportBlank(dto);
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
                var data = await _submissionImportService.ImportBlank(dto);
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
                var data = await _submissionImportService.VerifyImportOverwrite(dto);
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
                var data = await _submissionImportService.ImportOverwrite(dto);
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

        [HttpGet("{id:int}/neighbor")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_VIEW)]
        public async Task<ApiResponse> FindNeighbor([FromRoute] int id, [FromQuery] FindSubmissionNeighborDto dto)
        {
            try
            {
                var data = await _submissionService.FindNeighbor(id, dto);
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
