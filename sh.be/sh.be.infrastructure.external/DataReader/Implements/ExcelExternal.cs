using ClosedXML.Excel;
using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Base.Interfaces.External.DataReader;
using sh.be.infrastructure.external.DataReader.Base;

namespace sh.be.infrastructure.external.DataReader.Implements
{
    public class ExcelExternal : IExcelExternal
    {
        private const string TemplateFileSuffix = "_template";
        private const int HeaderRowIndex = 1;
        private const int AdjustWidthRowCount = 200;
        private const double MinColumnWidth = 8;
        private const double MaxColumnWidth = 60;

        public ExportFileDto CreateTemplate(string templateName, IReadOnlyList<string> headers)
        {
            return CreateWorkbook(ExcelNaming.ToFileName(templateName, TemplateFileSuffix), workbook => AddWorksheet(workbook, templateName, headers, []));
        }

        public ExportFileDto CreateMultiSheetTemplate(string templateName, IReadOnlyList<ExcelSheetTemplateDto> sheets)
        {
            return CreateWorkbook(ExcelNaming.ToFileName(templateName, TemplateFileSuffix), workbook =>
            {
                foreach (var sheet in sheets)
                {
                    AddWorksheet(workbook, sheet.SheetName, sheet.Headers, []);
                }
            });
        }

        public ExportFileDto CreateTable(string fileName, string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
        {
            return CreateWorkbook(ExcelNaming.ToFileName(fileName), workbook => AddWorksheet(workbook, sheetName, headers, rows));
        }

        private static ExportFileDto CreateWorkbook(string fileName, Action<XLWorkbook> addWorksheets)
        {
            using var workbook = new XLWorkbook();
            addWorksheets(workbook);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return new ExportFileDto
            {
                FileName = fileName,
                ContentType = ExcelNaming.ContentType,
                Content = stream.ToArray(),
            };
        }

        private static void AddWorksheet(XLWorkbook workbook, string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
        {
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
        }
    }
}
