using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.Plugin;
using sh.be.applications.Base.Interfaces;
using sh.be.applications.Module.DigitalSignature.Dtos.Plugin;
using sh.be.applications.Module.DigitalSignature.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Core.Repositories;
using sh.be.domain.Module.DigitalSignature.Services;

namespace sh.be.applications.Module.DigitalSignature.Implements
{
    public class PluginService : BaseService, IPluginService
    {
        private readonly IPluginSetupProvider _pluginSetupProvider;

        public PluginService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<PluginService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IPluginSetupProvider pluginSetupProvider
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _pluginSetupProvider = pluginSetupProvider;
        }

        public PluginVersionDto CheckVersion(CheckPluginVersionDto dto)
        {
            _logger.LogInformation($"{nameof(CheckVersion)}, version={dto.Version}");
            var version = dto.Version?.Trim() ?? String.Empty;
            var supportedVersions = PluginVersionPolicy.ResolveSupportedVersions(_pluginSetupProvider.GetSupportedVersions());
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
                FileName = _pluginSetupProvider.GetSetupFileName(),
                Exists = _pluginSetupProvider.ExistsSetupFile(),
            };
        }

        public PluginSetupFileDto GetSetupContent()
        {
            _logger.LogInformation($"{nameof(GetSetupContent)}");
            return _pluginSetupProvider.OpenSetupFile();
        }
    }
}
