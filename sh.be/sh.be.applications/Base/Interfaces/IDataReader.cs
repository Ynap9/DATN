using sh.be.applications.Base.Dtos.DataReader;
using sh.be.domain.Base.Models;

namespace sh.be.applications.Base.Interfaces
{
    public interface IDataReader
    {
        Task<List<SheetHeaderDto>> GetSheetHeadersAsync(string source);
        Task<List<string>> GetHeadersAsync(string source, string sheetName, int headerRowIndex);
        Task<SheetTable> ReadTableAsync(string source, string sheetName);
    }
}
