using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Base;
using sh.be.applications.Module.Core.Interfaces.Services;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Core
{
    [Route("api/v{version:apiVersion}/core/signalr")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    public class SignalRController : BaseController
    {
        private readonly ISignalRService _signalRService;
        public SignalRController(ILogger<SignalRController> logger, ISignalRService signalRService) : base(logger)
        {
            _signalRService = signalRService;
        }

        [HttpGet("connection")]
        [AllowAnonymous]
        public ApiResponse CheckConnection()
        {
            try
            {
                var data = _signalRService.CheckConnection();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("ping")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ApiResponse> Ping()
        {
            try
            {
                var data = await _signalRService.Ping();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
