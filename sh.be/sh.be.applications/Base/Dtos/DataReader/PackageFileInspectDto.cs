namespace sh.be.applications.Base.Dtos.DataReader
{
    public class PackageFileInspectDto
    {
        public string Sha256 { get; set; } = String.Empty;
        public bool IsSigned { get; set; }
        public string? PdfAPart { get; set; }
        public int? PageCount { get; set; }
    }
}
