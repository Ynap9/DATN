using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Base.Interfaces;
using sh.be.domain.Base.Models;
using sh.be.infrastructure.external.DataReader.Models;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.infrastructure.external.DataReader.Base
{
    public abstract class DataReaderBase : IDataReader
    {
        private const int FirstDataRowNumber = 2;

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

        public async Task<List<string>> GetHeadersAsync(string source, string sheetName, int headerRowIndex)
        {
            if (headerRowIndex < 1)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Dòng header phải lớn hơn hoặc bằng 1");
            }
            var cells = await ReadHeaderCellsAsync(source, sheetName, headerRowIndex);
            return NormalizeHeaders(cells);
        }

        public async Task<SheetTable> ReadTableAsync(string source, string sheetName)
        {
            var rows = await ReadRowsAsync(source, sheetName);
            var headers = rows.Count > 0 ? NormalizeCells(rows[0]) : new List<string>();
            var dataRows = rows
                .Skip(1)
                .Select((cells, index) => new SheetRow(index + FirstDataRowNumber, NormalizeCells(cells)))
                .ToList();
            return new SheetTable(headers, dataRows);
        }

        protected abstract Task<List<SheetHeaderRow>> ReadHeaderRowsAsync(string source);

        protected abstract Task<List<string?>> ReadHeaderCellsAsync(string source, string sheetName, int headerRowIndex);

        protected abstract Task<List<List<string?>>> ReadRowsAsync(string source, string sheetName);

        private static List<string> NormalizeCells(IEnumerable<string?> cells)
        {
            return cells
                .Select(cell => cell?.Trim() ?? String.Empty)
                .ToList();
        }

        private static List<string> NormalizeHeaders(IEnumerable<string?> cells)
        {
            return cells
                .Where(cell => !string.IsNullOrWhiteSpace(cell))
                .Select(cell => cell!.Trim())
                .ToList();
        }
    }
}
