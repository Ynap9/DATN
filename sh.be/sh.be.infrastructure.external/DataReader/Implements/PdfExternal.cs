using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Base.Interfaces.External.DataReader;
using sh.be.infrastructure.external.DigitalSignature.Base;
using sh.be.shared.Constants.DigitalSignature;

namespace sh.be.infrastructure.external.DataReader.Implements
{
    public class PdfExternal : IPdfExternal
    {
        private const string ContentType = "application/pdf";
        private const string PdfExtension = ".pdf";
        private const string DefaultFileName = "report";
        private const string FontFamily = TemplateConst.DEFAULT_DIGITAL_SIGNATURE_FONT;
        private const double TitleFontSize = 14;
        private const double BodyFontSize = 10;
        private const double PageMargin = 40;
        private const double CellPadding = 4;
        private const double SectionSpacing = 12;
        private const double MinColumnWidth = 40;
        private const double MaxColumnWidth = 200;
        private static readonly XPen BorderPen = new(XColors.Black, 0.5);
        private static readonly XBrush HeaderBrush = new XSolidBrush(XColor.FromArgb(230, 230, 230));

        // Dựng PDF A4 dọc: tiêu đề căn giữa, các dòng thông tin, các bảng kẻ ô tự sang trang và lặp hàng tiêu đề
        public ExportFileDto CreateReport(string fileName, PdfReportDto report)
        {
            PdfSharpSetup.EnsureFontResolver(new AppearanceFontResolver());
            lock (PdfSharpSetup.Lock)
            {
                using var document = new PdfDocument();
                var titleFont = new XFont(FontFamily, TitleFontSize, XFontStyleEx.Bold);
                var boldFont = new XFont(FontFamily, BodyFontSize, XFontStyleEx.Bold);
                var regularFont = new XFont(FontFamily, BodyFontSize);
                var lineHeight = regularFont.GetHeight();

                var gfx = AddPage(document, null);
                var contentWidth = gfx.PageSize.Width - PageMargin * 2;
                var bottom = gfx.PageSize.Height - PageMargin;
                var y = PageMargin;

                foreach (var line in WrapText(gfx, report.Title, titleFont, contentWidth))
                {
                    gfx.DrawString(line, titleFont, XBrushes.Black, new XRect(PageMargin, y, contentWidth, titleFont.GetHeight()), XStringFormats.TopCenter);
                    y += titleFont.GetHeight();
                }
                y += SectionSpacing;

                foreach (var line in report.InfoLines.SelectMany(infoLine => WrapText(gfx, infoLine, regularFont, contentWidth)))
                {
                    if (y + lineHeight > bottom)
                    {
                        gfx = AddPage(document, gfx);
                        y = PageMargin;
                    }
                    gfx.DrawString(line, regularFont, XBrushes.Black, new XRect(PageMargin, y, contentWidth, lineHeight), XStringFormats.TopLeft);
                    y += lineHeight;
                }

                foreach (var table in report.Tables)
                {
                    // Độ rộng cột theo chữ dài nhất của cột, kẹp trần / sàn rồi co giãn cho vừa khổ giấy
                    var naturalWidths = table.Headers
                        .Select((header, columnIndex) => Math.Clamp(
                            Math.Max(gfx.MeasureString(header, boldFont).Width,
                                table.Rows.Select(row => columnIndex < row.Count ? gfx.MeasureString(row[columnIndex], regularFont).Width : 0).DefaultIfEmpty(0).Max())
                            + CellPadding * 2,
                            MinColumnWidth, MaxColumnWidth))
                        .ToList();
                    var scale = contentWidth / naturalWidths.Sum();
                    var columnWidths = naturalWidths.Select(width => width * scale).ToList();

                    var headerLines = table.Headers.Select((header, columnIndex) => WrapText(gfx, header, boldFont, columnWidths[columnIndex] - CellPadding * 2)).ToList();
                    var headerHeight = headerLines.Max(lines => lines.Count) * lineHeight + CellPadding * 2;

                    y += SectionSpacing;
                    if (y + lineHeight + headerHeight > bottom)
                    {
                        gfx = AddPage(document, gfx);
                        y = PageMargin;
                    }
                    gfx.DrawString(table.Heading, boldFont, XBrushes.Black, new XRect(PageMargin, y, contentWidth, lineHeight), XStringFormats.TopLeft);
                    y += lineHeight + CellPadding;
                    DrawRow(gfx, y, headerHeight, headerLines, columnWidths, boldFont, true);
                    y += headerHeight;

                    foreach (var row in table.Rows)
                    {
                        var cellLines = columnWidths.Select((width, columnIndex) => WrapText(gfx, columnIndex < row.Count ? row[columnIndex] : String.Empty, regularFont, width - CellPadding * 2)).ToList();
                        var rowHeight = cellLines.Max(lines => lines.Count) * lineHeight + CellPadding * 2;
                        if (y + rowHeight > bottom)
                        {
                            gfx = AddPage(document, gfx);
                            y = PageMargin;
                            DrawRow(gfx, y, headerHeight, headerLines, columnWidths, boldFont, true);
                            y += headerHeight;
                        }
                        DrawRow(gfx, y, rowHeight, cellLines, columnWidths, regularFont, false);
                        y += rowHeight;
                    }
                }
                gfx.Dispose();

                using var stream = new MemoryStream();
                document.Save(stream, false);
                var safeFileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars())).Trim();
                return new ExportFileDto
                {
                    FileName = (string.IsNullOrWhiteSpace(safeFileName) ? DefaultFileName : safeFileName) + PdfExtension,
                    ContentType = ContentType,
                    Content = stream.ToArray(),
                };
            }
        }

        // Đóng trang đang vẽ (nếu có) và mở trang A4 dọc mới
        private static XGraphics AddPage(PdfDocument document, XGraphics? previousGfx)
        {
            previousGfx?.Dispose();
            var page = document.AddPage();
            page.Size = PageSize.A4;
            page.Orientation = PageOrientation.Portrait;
            return XGraphics.FromPdfPage(page);
        }

        // Xuống dòng theo từ cho vừa bề rộng; từ dài hơn cả dòng thì cắt theo ký tự
        private static List<string> WrapText(XGraphics gfx, string text, XFont font, double maxWidth)
        {
            var lines = new List<string>();
            foreach (var paragraph in text.Replace("\r", String.Empty).Split('\n'))
            {
                var line = String.Empty;
                foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var candidate = line.Length == 0 ? word : $"{line} {word}";
                    if (gfx.MeasureString(candidate, font).Width <= maxWidth)
                    {
                        line = candidate;
                        continue;
                    }
                    if (line.Length > 0)
                    {
                        lines.Add(line);
                    }
                    line = String.Empty;
                    foreach (var character in word)
                    {
                        if (line.Length > 0 && gfx.MeasureString(line + character, font).Width > maxWidth)
                        {
                            lines.Add(line);
                            line = String.Empty;
                        }
                        line += character;
                    }
                }
                lines.Add(line);
            }
            return lines;
        }

        // Vẽ một hàng bảng: nền xám cho hàng tiêu đề, khung từng ô, các dòng chữ đã xuống dòng
        private static void DrawRow(XGraphics gfx, double y, double rowHeight, List<List<string>> cellLines, List<double> columnWidths, XFont font, bool isHeader)
        {
            var lineHeight = font.GetHeight();
            var x = PageMargin;
            for (var columnIndex = 0; columnIndex < columnWidths.Count; columnIndex++)
            {
                var cell = new XRect(x, y, columnWidths[columnIndex], rowHeight);
                if (isHeader)
                {
                    gfx.DrawRectangle(HeaderBrush, cell);
                }
                gfx.DrawRectangle(BorderPen, cell);
                for (var lineIndex = 0; lineIndex < cellLines[columnIndex].Count; lineIndex++)
                {
                    gfx.DrawString(cellLines[columnIndex][lineIndex], font, XBrushes.Black,
                        new XRect(x + CellPadding, y + CellPadding + lineIndex * lineHeight, columnWidths[columnIndex] - CellPadding * 2, lineHeight), XStringFormats.TopLeft);
                }
                x += columnWidths[columnIndex];
            }
        }
    }
}
