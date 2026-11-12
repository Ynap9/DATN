using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base.Interfaces.External.DataReader;
using sh.be.applications.Base.Interfaces.External.Storage;
using sh.be.infrastructure.external.DataReader.Base;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.infrastructure.external.DataReader.Implements
{
    public class ExcelReaderExternal : DataReaderExternalBase, IExcelReaderExternal
    {
        private const int FirstRowNumber = 1;

        private readonly IFileStorageExternal _fileStorageExternal;
        private readonly ILogger<ExcelReaderExternal> _logger;

        public ExcelReaderExternal(IFileStorageExternal fileStorageExternal, ILogger<ExcelReaderExternal> logger)
        {
            _fileStorageExternal = fileStorageExternal;
            _logger = logger;
        }

        protected override async Task<List<SheetHeaderRow>> ReadHeaderRowsAsync(string source)
        {
            _logger.LogInformation($"{nameof(ReadHeaderRowsAsync)}, source={source}");
            using var workbook = await OpenWorkbookAsync(source);
            return workbook.Worksheets
                .Select(worksheet => new SheetHeaderRow
                {
                    SheetName = worksheet.Name,
                    Cells = ReadRowCells(worksheet.Row(FirstRowNumber)),
                })
                .ToList();
        }

        protected override async Task<List<string?>> ReadHeaderCellsAsync(string source, string sheetName, int headerRowIndex)
        {
            _logger.LogInformation($"{nameof(ReadHeaderCellsAsync)}, source={source}, sheetName={sheetName}, headerRowIndex={headerRowIndex}");
            using var workbook = await OpenWorkbookAsync(source);
            return ReadRowCells(FindWorksheet(workbook, sheetName).Row(headerRowIndex));
        }

        protected override async Task<List<List<string?>>> ReadRowsAsync(string source, string sheetName)
        {
            _logger.LogInformation($"{nameof(ReadRowsAsync)}, source={source}, sheetName={sheetName}");
            using var workbook = await OpenWorkbookAsync(source);
            var worksheet = FindWorksheet(workbook, sheetName);

            // Giữ cả dòng trống ở giữa như Google Sheet trả về, để base đánh số dòng theo vị trí vẫn khớp số dòng thật trong Excel.
            var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            var rows = Enumerable.Range(0, lastRowNumber).Select(_ => new List<string?>()).ToList();
            foreach (var row in worksheet.RowsUsed())
            {
                rows[row.RowNumber() - FirstRowNumber] = ReadRowCells(row);
            }
            return rows;
        }

        private async Task<XLWorkbook> OpenWorkbookAsync(string fileKey)
        {
            byte[] content;
            try
            {
                content = await _fileStorageExternal.DownloadAsync(fileKey);
            }
            catch (UserFriendlyException ex) when (ex.ErrorCode == ErrorCodes.CoreErrorDownloadFileFailed)
            {
                // Kho lưu trữ không tách "không có object" khỏi lỗi khác; file import quá hạn đã bị job dọn xoá nên báo hết hạn.
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorImportFileInvalid, "File không tồn tại hoặc đã hết hạn");
            }

            try
            {
                using var stream = new MemoryStream(content);
                return new XLWorkbook(stream);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"{nameof(OpenWorkbookAsync)}, fileKey={fileKey}");
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorImportFileInvalid, "Không đọc được file, chỉ nhận file Excel .xlsx");
            }
        }

        private static IXLWorksheet FindWorksheet(XLWorkbook workbook, string sheetName)
        {
            return workbook.Worksheets.FirstOrDefault(worksheet => worksheet.Name == sheetName)
                ?? throw new UserFriendlyException(ErrorCodes.CoreErrorSheetNotFound);
        }

        // Lấy chuỗi đúng như người dùng thấy trong Excel (ngày, số, kết quả công thức theo định dạng ô); bỏ ô trống cuối dòng như Google Sheet.
        private static List<string?> ReadRowCells(IXLRow row)
        {
            var lastCell = row.LastCellUsed();
            if (lastCell == null)
            {
                return new List<string?>();
            }

            var cells = new List<string?>(new string?[lastCell.Address.ColumnNumber]);
            foreach (var cell in row.CellsUsed())
            {
                cells[cell.Address.ColumnNumber - 1] = cell.GetFormattedString();
            }
            return cells;
        }
    }
}
