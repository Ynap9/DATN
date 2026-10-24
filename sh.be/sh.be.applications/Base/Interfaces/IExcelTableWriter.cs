using sh.be.applications.Base.Dtos.Excel;

namespace sh.be.applications.Base.Interfaces
{
    public interface IExcelTableWriter
    {
        ExportFileDto CreateTable(string fileName, string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows);
    }
}
