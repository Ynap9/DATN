namespace sh.be.infrastructure.external.DataReader.Models
{
    public class SheetHeaderRow
    {
        public string SheetName { get; set; } = String.Empty;
        public List<string?> Cells { get; set; } = new();
    }
}
