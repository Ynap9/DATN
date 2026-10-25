using sh.be.applications.Base.Dtos.DataReader;

namespace sh.be.applications.Base.Interfaces.External.DataReader
{
    public interface IExcelExternal
    {
        ExportFileDto CreateTemplate(string templateName, IReadOnlyList<string> headers);
        ExportFileDto CreateMultiSheetTemplate(string templateName, IReadOnlyList<ExcelSheetTemplateDto> sheets);
        ExportFileDto CreateTable(string fileName, string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows);
    }
}
