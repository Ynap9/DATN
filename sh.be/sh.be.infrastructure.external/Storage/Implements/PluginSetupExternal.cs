using Microsoft.Extensions.Options;
using sh.be.applications.Base.Dtos.Storage;
using sh.be.applications.Base.Interfaces.External.Storage;
using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Settings;
using System.Diagnostics;

namespace sh.be.infrastructure.external.Storage.Implements
{
    public class PluginSetupExternal : IPluginSetupExternal
    {
        private static readonly string SetupFilePath = Path.Combine(AppContext.BaseDirectory, PluginConst.SETUP_FOLDER, PluginConst.SETUP_FILE_NAME);

        private readonly IOptionsMonitor<PluginSettings> _pluginSettings;

        public PluginSetupExternal(IOptionsMonitor<PluginSettings> pluginSettings)
        {
            _pluginSettings = pluginSettings;
        }

        public IReadOnlyList<string> GetSupportedVersions()
        {
            return _pluginSettings.CurrentValue.SupportedVersions;
        }

        public bool ExistsSetupFile()
        {
            return File.Exists(SetupFilePath);
        }

        public string GetSetupFileName()
        {
            var version = ExistsSetupFile()
                ? FileVersionInfo.GetVersionInfo(SetupFilePath).ProductVersion?.Split('+')[0]
                : null;
            return string.IsNullOrWhiteSpace(version)
                ? PluginConst.SETUP_FILE_NAME
                : $"{Path.GetFileNameWithoutExtension(PluginConst.SETUP_FILE_NAME)} {version}{Path.GetExtension(PluginConst.SETUP_FILE_NAME)}";
        }

        public PluginSetupFileDto OpenSetupFile()
        {
            if (!ExistsSetupFile())
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPluginSetupMissing);
            }
            return new PluginSetupFileDto
            {
                FileName = GetSetupFileName(),
                ContentType = PluginConst.SETUP_CONTENT_TYPE,
                Content = File.OpenRead(SetupFilePath),
            };
        }
    }
}
