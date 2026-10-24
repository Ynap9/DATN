using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.Storage;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Base.Interfaces.External.Storage;
using sh.be.applications.Module.DigitalSignature.Dtos.Plugin;
using sh.be.applications.Module.DigitalSignature.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Core.Repositories;
using sh.be.domain.Module.DigitalSignature.Services;

namespace sh.be.applications.Module.DigitalSignature.Implements
{
    public class PluginService : BaseService, IPluginService
    {
        private readonly IPluginSetupExternal _pluginSetupExternal;

        public PluginService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<PluginService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IPluginSetupExternal pluginSetupExternal
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _pluginSetupExternal = pluginSetupExternal;
        }

        public PluginVersionDto CheckVersion(CheckPluginVersionDto dto)
        {
            _logger.LogInformation($"{nameof(CheckVersion)}, version={dto.Version}");
            var version = dto.Version?.Trim() ?? String.Empty;
            var supportedVersions = PluginVersionPolicy.ResolveSupportedVersions(_pluginSetupExternal.GetSupportedVersions());
            var reason = PluginVersionPolicy.ResolveUnsupportedReason(version, supportedVersions);

            return new PluginVersionDto
            {
                Version = version,
                IsSupported = reason == null,
                SupportedVersions = supportedVersions,
                Reason = reason,
            };
        }

        public PluginSetupDto GetSetup()
        {
            _logger.LogInformation($"{nameof(GetSetup)}");
            return new PluginSetupDto
            {
                FileName = _pluginSetupExternal.GetSetupFileName(),
                Exists = _pluginSetupExternal.ExistsSetupFile(),
            };
        }

        public PluginSetupFileDto GetSetupContent()
        {
            _logger.LogInformation($"{nameof(GetSetupContent)}");
            return _pluginSetupExternal.OpenSetupFile();
        }
    }
}
