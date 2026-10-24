namespace sh.be.applications.Base.Dtos.DataReader
{
    public class ExportFileDto
    {
        public string FileName { get; set; } = String.Empty;
        public string ContentType { get; set; } = String.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}
