namespace sh.be.applications.Base.Dtos.Storage
{
    public class PackageArchiveDto
    {
        public int PackageType { get; set; }
        public string Objid { get; set; } = String.Empty;
        public string PackageCode { get; set; } = String.Empty;
        public string FondsCode { get; set; } = String.Empty;
        public string BuiltBy { get; set; } = String.Empty;
        public string? ArchiveCode { get; set; }
        public string? ArchiveName { get; set; }
        public string MetadataFilePrefix { get; set; } = String.Empty;
        public List<PackageArchiveElementDto> Metadata { get; set; } = new();
        public List<PackageArchiveSchemaDto> Schemas { get; set; } = new();
        public List<PackageArchiveFileDto> Files { get; set; } = new();
        public PackageArchiveFileDto? DocumentationFile { get; set; }
    }

    public class PackageArchiveSchemaDto
    {
        public string XsdFileName { get; set; } = String.Empty;
        public List<string> ElementNames { get; set; } = new();
    }

    public class PackageArchiveFileDto
    {
        public int MetadataType { get; set; }
        public string DocKey { get; set; } = String.Empty;
        public string Extension { get; set; } = String.Empty;
        public string MimeType { get; set; } = String.Empty;
        public string ObjectKey { get; set; } = String.Empty;
        public string Sha256 { get; set; } = String.Empty;
        public string MetadataFilePrefix { get; set; } = String.Empty;
        public List<PackageArchiveElementDto> Metadata { get; set; } = new();
    }

    public class PackageArchiveElementDto
    {
        public string ElementName { get; set; } = String.Empty;
        public string Value { get; set; } = String.Empty;
    }
}
