using sh.be.applications.Base.Dtos.Storage;
using sh.be.applications.Module.DigitalSignature.Dtos.Plugin;

namespace sh.be.applications.Module.DigitalSignature.Interfaces.Services
{
    public interface IPluginService
    {
        PluginVersionDto CheckVersion(CheckPluginVersionDto dto);
        PluginSetupDto GetSetup();
        PluginSetupFileDto GetSetupContent();
    }
}
