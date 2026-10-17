using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Base;
using sh.be.applications.Module.DigitalSignature.Dtos.Plugin;
using sh.be.applications.Module.DigitalSignature.Interfaces.Services;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.DigitalSignature
{
    [Route("api/digital-signature/plugin")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PluginController : BaseController
    {
        private readonly IPluginService _pluginService;
        public PluginController(ILogger<PluginController> logger, IPluginService pluginService) : base(logger)
        {
            _pluginService = pluginService;
        }

        [HttpGet("version")]
        public ApiResponse CheckVersion([FromQuery] CheckPluginVersionDto dto)
        {
            try
            {
                var data = _pluginService.CheckVersion(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("setup")]
        public ApiResponse GetSetup()
        {
            try
            {
                var data = _pluginService.GetSetup();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("setup/content")]
        public IActionResult GetSetupContent()
        {
            try
            {
                var file = _pluginService.GetSetupContent();
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                return Ok(OkException(ex));
            }
        }
    }
}
