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

        [HttpPut("check")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_CHECK)]
        public async Task<ApiResponse> Check([FromBody] CheckSubmissionDto dto)
        {
            try
            {
                var data = await _submissionService.Check(dto);
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

        [HttpGet("qa/paging")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_VIEW)]
        public async Task<ApiResponse> FindQaPaging([FromQuery] FindPagingQaSubmissionDto dto)
        {
            try
            {
                var data = await _submissionService.FindQaPaging(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id:int}/qa")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_VIEW)]
        public async Task<ApiResponse> FindQaById([FromRoute] int id, [FromQuery] FindQaSubmissionDetailDto dto)
        {
            try
            {
                var data = await _submissionService.FindQaById(id, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPut("qa")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_QA)]
        public async Task<ApiResponse> ReviewQa([FromBody] ReviewQaSubmissionDto dto)
        {
            try
            {
                var data = await _submissionService.ReviewQa(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id:int}/qa-logs")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_VIEW)]
        public async Task<ApiResponse> FindQaLogPaging([FromRoute] int id, [FromQuery] FindPagingSubmissionQaLogDto dto)
        {
            try
            {
                var data = await _submissionService.FindQaLogPaging(id, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("field-values")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_VIEW)]
        public async Task<ApiResponse> FindFieldValues([FromQuery] FindSubmissionFieldValueDto dto)
        {
            try
            {
                var data = await _submissionService.FindFieldValues(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("tree")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_VIEW)]
        public async Task<ApiResponse> FindTree([FromQuery] FindSubmissionTreeDto dto)
        {
            try
            {
                var data = await _submissionService.FindTree(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("logs")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_VIEW)]
        public async Task<ApiResponse> FindProjectLogPaging([FromQuery] FindPagingProjectSubmissionLogDto dto)
        {
            try
            {
                var data = await _submissionService.FindProjectLogPaging(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("summary")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_VIEW)]
        public async Task<ApiResponse> FindSummary([FromQuery] FindSubmissionSummaryDto dto)
        {
            try
            {
                var data = await _submissionService.FindSummary(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpDelete("")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_DELETE)]
        public async Task<ApiResponse> Delete([FromBody] DeleteSubmissionDto dto)
        {
            try
            {
                var data = await _submissionService.Delete(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("export-excel")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_EXPORT)]
        public async Task<IActionResult> ExportExcel([FromQuery] ExportSubmissionDto dto)
        {
            try
            {
                var file = await _submissionService.ExportExcel(dto);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                return Ok(OkException(ex));
            }
        }

        [HttpPost("export-excel/job")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_EXPORT)]
        public async Task<ApiResponse> CreateExportJob([FromQuery] ExportSubmissionDto dto)
        {
            try
            {
                var data = await _submissionService.CreateExportJob(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("export-excel/job/{jobId}")]
        [Permission(PermissionConstants.FUNCTION_SUBMISSION_EXPORT)]
        public async Task<ApiResponse> FindExportJob([FromRoute] string jobId)
        {
            try
            {
                var data = await _submissionService.FindExportJob(jobId);
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
