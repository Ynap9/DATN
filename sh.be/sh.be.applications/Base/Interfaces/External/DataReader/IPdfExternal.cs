using sh.be.applications.Base.Dtos.DataReader;

namespace sh.be.applications.Base.Interfaces.External.DataReader
{
    public interface IPdfExternal
    {
        ExportFileDto CreateReport(string fileName, PdfReportDto report);
    }
}
