namespace sh.be.applications.Module.Packaging.Dtos.PackageConfig
{
    public class GetListPackageSchemaDropDownDto
    {
        public int PackageSchemaId { get; set; }
        public int PackageType { get; set; }
        public int MetadataType { get; set; }
        public string Name { get; set; } = String.Empty;
    }
}
