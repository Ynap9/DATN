using ClosedXML.Excel;
using sh.be.applications.Base.Dtos.Excel;
using sh.be.applications.Base.Interfaces;
using sh.be.infrastructure.external.Excel.Base;

namespace sh.be.infrastructure.external.Excel.Implements
{
    public class ExcelTemplateWriter : IExcelTemplateWriter
    {
        private const string TemplateFileSuffix = "_template";
        private const int HeaderRowIndex = 1;

        public ExportFileDto CreateTemplate(string templateName, IReadOnlyList<string> headers)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(ExcelNaming.ToSheetName(templateName));

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
                FileName = ExcelNaming.ToFileName(templateName, TemplateFileSuffix),
                ContentType = ExcelNaming.ContentType,
                Content = stream.ToArray(),
            };
        }
    }
}
