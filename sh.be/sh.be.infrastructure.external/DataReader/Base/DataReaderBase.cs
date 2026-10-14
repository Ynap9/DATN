using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Base.Interfaces;
using sh.be.infrastructure.external.DataReader.Models;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

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

        public async Task<List<string>> GetHeadersAsync(string source, string sheetName, int headerRowIndex)
        {
            if (headerRowIndex < 1)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Dòng header phải lớn hơn hoặc bằng 1");
            }
            var cells = await ReadHeaderCellsAsync(source, sheetName, headerRowIndex);
            return NormalizeHeaders(cells);
        }

        protected abstract Task<List<SheetHeaderRow>> ReadHeaderRowsAsync(string source);

        protected abstract Task<List<string?>> ReadHeaderCellsAsync(string source, string sheetName, int headerRowIndex);

        private static List<string> NormalizeHeaders(IEnumerable<string?> cells)
        {
            return cells
                .Where(cell => !string.IsNullOrWhiteSpace(cell))
                .Select(cell => cell!.Trim())
                .ToList();
        }
    }
}
