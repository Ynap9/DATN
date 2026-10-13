namespace sh.be.applications.Base.Dtos.DataReader
{
    public class SheetHeaderDto
    {
        public string SheetName { get; set; } = String.Empty;
        public List<string> Headers { get; set; } = new();
    }
}
