namespace sh.be.applications.Base.Dtos.File
{
    public class FileStreamDto
    {
        public Stream Content { get; set; } = Stream.Null;
        public string ContentType { get; set; } = String.Empty;
        public string FileName { get; set; } = String.Empty;
    }
}
