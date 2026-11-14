namespace sh.be.applications.Base.Dtos.DataReader
{
    public class PdfReportDto
    {
        public string Title { get; set; } = String.Empty;
        public List<string> InfoLines { get; set; } = [];
        public List<PdfReportTableDto> Tables { get; set; } = [];
    }

    public class PdfReportTableDto
    {
        public string Heading { get; set; } = String.Empty;
        public List<string> Headers { get; set; } = [];
        public List<List<string>> Rows { get; set; } = [];
    }
}
