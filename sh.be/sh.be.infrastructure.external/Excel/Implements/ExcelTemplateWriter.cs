using ClosedXML.Excel;
using sh.be.applications.Base.Dtos.Excel;
using sh.be.applications.Base.Interfaces;
using System.Text.RegularExpressions;

namespace sh.be.infrastructure.external.Excel.Implements
{
    public class ExcelTemplateWriter : IExcelTemplateWriter
    {
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string ExcelExtension = ".xlsx";
        private const string TemplateFileSuffix = "_template";
        private const string DefaultSheetName = "Sheet1";
        private const int MaxSheetNameLength = 31;
        private const int HeaderRowIndex = 1;
        private static readonly Regex InvalidSheetNameCharsRegex = new(@"[\[\]:*?/\\]", RegexOptions.Compiled);

        public ExportFileDto CreateTemplate(string templateName, IReadOnlyList<string> headers)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(ToSheetName(templateName));

            for (var index = 0; index < headers.Count; index++)
            {
                worksheet.Cell(HeaderRowIndex, index + 1).Value = headers[index];
            }
            worksheet.Row(HeaderRowIndex).Style.Font.Bold = true;
            worksheet.SheetView.FreezeRows(HeaderRowIndex);
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return new ExportFileDto
            {
                FileName = ToFileName(templateName),
                ContentType = ExcelContentType,
                Content = stream.ToArray(),
            };
        }

        private static string ToSheetName(string templateName)
        {
            var sheetName = InvalidSheetNameCharsRegex.Replace(templateName, "_").Trim().Trim('\'');
            if (sheetName.Length > MaxSheetNameLength)
            {
                sheetName = sheetName[..MaxSheetNameLength];
            }
            return string.IsNullOrWhiteSpace(sheetName) ? DefaultSheetName : sheetName;
        }

        private static string ToFileName(string templateName)
        {
            var fileName = string.Join("_", templateName.Split(Path.GetInvalidFileNameChars())).Trim();
            return (string.IsNullOrWhiteSpace(fileName) ? DefaultSheetName : fileName) + TemplateFileSuffix + ExcelExtension;
        }
    }
}
