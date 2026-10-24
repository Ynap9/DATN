using sh.be.applications.Base.Dtos.DataReader;

namespace sh.be.applications.Base.Interfaces.External.Storage
{
    public interface ISampleFileExternal
    {
        ExportFileDto ReadSigningSampleFile();
    }
}
