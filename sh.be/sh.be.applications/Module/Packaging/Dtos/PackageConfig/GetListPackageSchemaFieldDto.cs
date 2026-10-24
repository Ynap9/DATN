namespace sh.be.applications.Module.Packaging.Dtos.PackageConfig
{
    public class GetListPackageSchemaFieldDto
    {
        public int Id { get; set; }
        public int PackageSchemaId { get; set; }
        public int PackageType { get; set; }
        public int MetadataType { get; set; }
        public string SchemaName { get; set; } = String.Empty;
        public string ElementName { get; set; } = String.Empty;
        public string DisplayName { get; set; } = String.Empty;
        public int DataType { get; set; }
        public int? MaxLength { get; set; }
        public int Requirement { get; set; }
        public string? ConditionElement { get; set; }
        public string? ConditionValues { get; set; }
        public string? CodeGroup { get; set; }
        public bool AllowMultipleValues { get; set; }
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public List<GetListPackageCodeValueDto> Codes { get; set; } = [];
    }
}
