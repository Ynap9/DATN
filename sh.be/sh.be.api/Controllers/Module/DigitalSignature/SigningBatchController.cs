using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Base.Dtos.Signing;
using sh.be.applications.Module.DigitalSignature.Dtos.SigningBatch;
using sh.be.applications.Module.DigitalSignature.Interfaces.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.DigitalSignature
{
    [Route("api/v{version:apiVersion}/digital-signature/signing-batch")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SigningBatchController : BaseController
    {
        private readonly ISigningBatchService _signingBatchService;
        public SigningBatchController(ILogger<SigningBatchController> logger, ISigningBatchService signingBatchService) : base(logger)
        {
            _signingBatchService = signingBatchService;
        }

        [HttpGet("running")]
        [Permission(PermissionConstants.FUNCTION_SIGNING_BATCH_VIEW)]
        public async Task<ApiResponse> FindRunning()
        {
            try
            {
                var data = await _signingBatchService.FindRunning();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}/files")]
        [Permission(PermissionConstants.FUNCTION_SIGNING_BATCH_VIEW)]
        public async Task<ApiResponse> GetFiles([FromRoute] int id)
        {
            try
            {
                var data = await _signingBatchService.GetFiles(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost]
        [Permission(PermissionConstants.FUNCTION_SIGNING_BATCH_CREATE)]
        public async Task<ApiResponse> Create([FromBody] CreateSigningBatchDto dto)
        {
            try
            {
                var data = await _signingBatchService.Create(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("{id}/files")]
        [Permission(PermissionConstants.FUNCTION_SIGNING_BATCH_CREATE)]
        [RequestSizeLimit(SigningBatchConst.MAX_UPLOAD_BYTES)]
        [RequestFormLimits(MultipartBodyLengthLimit = SigningBatchConst.MAX_UPLOAD_BYTES)]
        public async Task<ApiResponse> AddFiles([FromRoute] int id, [FromForm] AddFilesSigningBatchDto dto)
        {
            try
            {
                var data = await _signingBatchService.AddFiles(id, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("{id}/session")]
        [Permission(PermissionConstants.FUNCTION_SIGNING_BATCH_SIGN)]
        public async Task<ApiResponse> OpenSession([FromRoute] int id, [FromBody] OpenSessionSigningBatchDto dto)
        {
            try
            {
                var data = await _signingBatchService.OpenSession(id, dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpDelete("{id}/session")]
        [Permission(PermissionConstants.FUNCTION_SIGNING_BATCH_SIGN)]
        public async Task<ApiResponse> CloseSession([FromRoute] int id)
        {
            try
            {
                await _signingBatchService.CloseSession(id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("{id}/start")]
        [Permission(PermissionConstants.FUNCTION_SIGNING_BATCH_SIGN)]
        public async Task<ApiResponse> Start([FromRoute] int id)
        {
            try
            {
                var data = await _signingBatchService.Start(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("{id}/pause")]
        [Permission(PermissionConstants.FUNCTION_SIGNING_BATCH_SIGN)]
        public async Task<ApiResponse> Pause([FromRoute] int id)
        {
            try
            {
                var data = await _signingBatchService.Pause(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("{id}/cancel")]
        [Permission(PermissionConstants.FUNCTION_SIGNING_BATCH_SIGN)]
        public async Task<ApiResponse> Cancel([FromRoute] int id)
        {
            try
            {
                var data = await _signingBatchService.Cancel(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}/progress")]
        [Permission(PermissionConstants.FUNCTION_SIGNING_BATCH_VIEW)]
        public async Task<ApiResponse> GetProgress([FromRoute] int id)
        {
            try
            {
                var data = await _signingBatchService.GetProgress(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}/requests")]
        [Permission(PermissionConstants.FUNCTION_SIGNING_BATCH_SIGN)]
        public async Task<ApiResponse> TakeRequests([FromRoute] int id, CancellationToken cancellationToken)
        {
            try
            {
                var data = await _signingBatchService.TakeRequests(id, cancellationToken);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("{id}/signatures")]
        [Permission(PermissionConstants.FUNCTION_SIGNING_BATCH_SIGN)]
        public async Task<ApiResponse> SubmitSignatures([FromRoute] int id, [FromBody] List<SignatureResultDto> results, CancellationToken cancellationToken)
        {
            try
            {
                var data = await _signingBatchService.SubmitSignatures(id, results, cancellationToken);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}/files/{fileId}/signed-url")]
        [Permission(PermissionConstants.FUNCTION_SIGNING_BATCH_VIEW)]
        public async Task<ApiResponse> GetSignedFileUrl([FromRoute] int id, [FromRoute] int fileId)
        {
            try
            {
                var data = await _signingBatchService.GetSignedFileUrl(id, fileId);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpDelete("{id}/files/{fileId}")]
        [Permission(PermissionConstants.FUNCTION_SIGNING_BATCH_DELETE)]
        public async Task<ApiResponse> RemoveFile([FromRoute] int id, [FromRoute] int fileId)
        {
            try
            {
                var data = await _signingBatchService.RemoveFile(id, fileId);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
