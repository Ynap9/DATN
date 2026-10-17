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

        public SubmissionController(ILogger<SubmissionController> logger, ISubmissionImportService submissionImportService,
            ISubmissionFileService submissionFileService) : base(logger)
        {
            _submissionImportService = submissionImportService;
            _submissionFileService = submissionFileService;
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
