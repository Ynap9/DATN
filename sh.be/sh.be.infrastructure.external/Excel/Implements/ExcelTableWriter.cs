using ClosedXML.Excel;
using sh.be.applications.Base.Dtos.Excel;
using sh.be.applications.Base.Interfaces;
using sh.be.infrastructure.external.Excel.Base;

namespace sh.be.infrastructure.external.Excel.Implements
{
    public class ExcelTableWriter : IExcelTableWriter
    {
        private const int HeaderRowIndex = 1;
        private const int AdjustWidthRowCount = 200;
        private const double MinColumnWidth = 8;
        private const double MaxColumnWidth = 60;

        public ExportFileDto CreateTable(string fileName, string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(ExcelNaming.ToSheetName(sheetName));

            for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
            {
                worksheet.Cell(HeaderRowIndex, columnIndex + 1).SetValue(headers[columnIndex]);
            }

            var rowIndex = HeaderRowIndex;
            foreach (var row in rows)
            {
                rowIndex++;
                for (var columnIndex = 0; columnIndex < row.Count; columnIndex++)
                {
                    worksheet.Cell(rowIndex, columnIndex + 1).SetValue(row[columnIndex]);
                }
            }

            worksheet.Row(HeaderRowIndex).Style.Font.Bold = true;
            worksheet.SheetView.FreezeRows(HeaderRowIndex);
            worksheet.Columns(1, headers.Count).AdjustToContents(HeaderRowIndex, Math.Min(rowIndex, HeaderRowIndex + AdjustWidthRowCount),
                MinColumnWidth, MaxColumnWidth);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return new ExportFileDto
            {
                FileName = ExcelNaming.ToFileName(fileName),
                ContentType = ExcelNaming.ContentType,
                Content = stream.ToArray(),
            };
        }
    }
}
