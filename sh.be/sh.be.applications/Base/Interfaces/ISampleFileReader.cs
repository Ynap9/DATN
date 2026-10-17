using sh.be.applications.Base.Dtos.Excel;

namespace sh.be.applications.Base.Interfaces
{
    public interface ISampleFileReader
    {
        ExportFileDto ReadSigningSampleFile();
    }
}
