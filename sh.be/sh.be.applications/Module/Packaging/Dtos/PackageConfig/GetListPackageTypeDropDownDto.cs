namespace sh.be.applications.Module.Packaging.Dtos.PackageConfig
{
    public class GetListPackageTypeDropDownDto
    {
        public int PackageType { get; set; }
        public string Code { get; set; } = String.Empty;
        public string Name { get; set; } = String.Empty;
        public List<GetListPackageSchemaDropDownDto> Schemas { get; set; } = [];
    }
}
