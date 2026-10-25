namespace sh.be.applications.Base.Dtos.DataReader
{
    public class ExcelSheetTemplateDto
    {
        public string SheetName { get; set; } = String.Empty;
        public IReadOnlyList<string> Headers { get; set; } = [];
    }
}
