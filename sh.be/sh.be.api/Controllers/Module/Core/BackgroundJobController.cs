using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Base;
using sh.be.applications.Module.Core.Interfaces.Services;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Core
{
    [Route("api/v{version:apiVersion}/core/background-job")]
    [ApiController]
    [ApiVersion(ApiVersionConstants.V1)]
    public class BackgroundJobController : BaseController
    {
        private readonly IBackgroundJobService _backgroundJobService;
        public BackgroundJobController(ILogger<BackgroundJobController> logger, IBackgroundJobService backgroundJobService) : base(logger)
        {
            _backgroundJobService = backgroundJobService;
        }

        [HttpGet("connection")]
        [AllowAnonymous]
        public ApiResponse CheckConnection()
        {
            try
            {
                var data = _backgroundJobService.CheckConnection();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
