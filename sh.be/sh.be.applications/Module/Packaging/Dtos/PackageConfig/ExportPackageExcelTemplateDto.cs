using Microsoft.AspNetCore.Mvc;

namespace sh.be.applications.Module.Packaging.Dtos.PackageConfig
{
    public class ExportPackageExcelTemplateDto
    {
        [FromQuery(Name = "packageType")]
        public int PackageType { get; set; }

        [FromQuery(Name = "metadataTypes")]
        public List<int> MetadataTypes { get; set; } = [];
    }
}
