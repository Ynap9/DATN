namespace sh.be.applications.Module.Packaging.Dtos.PackageSession
{
    public class GetListPackageDocumentDto
    {
        public int Id { get; set; }
        public int? DossierId { get; set; }
        public string FileRole { get; set; } = String.Empty;
        public int? MetadataType { get; set; }
        public string RelativePath { get; set; } = String.Empty;
        public string FileName { get; set; } = String.Empty;
        public string Extension { get; set; } = String.Empty;
        public string MimeType { get; set; } = String.Empty;
        public long SizeBytes { get; set; }
        public string Sha256 { get; set; } = String.Empty;
        public string? DocKey { get; set; }
        public string Status { get; set; } = String.Empty;
        public string? Reason { get; set; }
        public bool IsSigned { get; set; }
        public string? PdfAPart { get; set; }
        public int? PageCount { get; set; }
    }
}
