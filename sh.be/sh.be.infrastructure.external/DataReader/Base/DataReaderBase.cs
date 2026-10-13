using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Base.Interfaces;
using sh.be.infrastructure.external.DataReader.Models;

namespace sh.be.infrastructure.external.DataReader.Base
{
    public abstract class DataReaderBase : IDataReader
    {
        public async Task<List<SheetHeaderDto>> GetSheetHeadersAsync(string source)
        {
            var headerRows = await ReadHeaderRowsAsync(source);
            return headerRows
                .Select(headerRow => new SheetHeaderDto
                {
                    SheetName = headerRow.SheetName,
                    Headers = NormalizeHeaders(headerRow.Cells),
                })
                .ToList();
        }

        protected abstract Task<List<SheetHeaderRow>> ReadHeaderRowsAsync(string source);

        private static List<string> NormalizeHeaders(IEnumerable<string?> cells)
        {
            return cells
                .Where(cell => !string.IsNullOrWhiteSpace(cell))
                .Select(cell => cell!.Trim())
                .ToList();
        }
    }
}
