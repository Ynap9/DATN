using sh.be.applications.Base.Dtos.Plugin;

namespace sh.be.applications.Base.Interfaces
{
    public interface IPluginSetupProvider
    {
        IReadOnlyList<string> GetSupportedVersions();
        bool ExistsSetupFile();
        string GetSetupFileName();
        PluginSetupFileDto OpenSetupFile();
    }
}
