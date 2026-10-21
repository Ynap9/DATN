namespace sh.be.infrastructure.external.Pdf.Models
{
    public class PdfDict
    {
        public List<KeyValuePair<string, object?>> Items { get; } = new();

        public PdfDict Add(string key, object? value)
        {
            Items.Add(new KeyValuePair<string, object?>(key, value));
            return this;
        }
    }
}
