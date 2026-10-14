using sh.be.applications.Base.Dtos.Excel;

namespace sh.be.applications.Base.Interfaces
{
    public interface IExcelTemplateWriter
    {
        ExportFileDto CreateTemplate(string templateName, IReadOnlyList<string> headers);
    }
}
