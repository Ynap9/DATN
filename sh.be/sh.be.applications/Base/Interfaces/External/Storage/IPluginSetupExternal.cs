using sh.be.applications.Base.Dtos.Storage;

namespace sh.be.applications.Base.Interfaces.External.Storage
{
    public interface IPluginSetupExternal
    {
        IReadOnlyList<string> GetSupportedVersions();
        bool ExistsSetupFile();
        string GetSetupFileName();
        PluginSetupFileDto OpenSetupFile();
    }
}
