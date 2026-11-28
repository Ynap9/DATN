namespace sh.be.applications.Base.Dtos.DataReader
{
    public class ExcelTableDto
    {
        public string SheetName { get; set; } = String.Empty;
        public List<string> Headers { get; set; } = new();
        public List<List<string>> Rows { get; set; } = new();
        // Cột (từ 0) canh phải, dùng cho cột số
        public List<int> RightAlignedColumnIndexes { get; set; } = new();
    }
}
