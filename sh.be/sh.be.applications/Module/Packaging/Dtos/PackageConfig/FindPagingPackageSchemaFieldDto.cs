using Microsoft.AspNetCore.Mvc;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Packaging.Dtos.PackageConfig
{
    public class FindPagingPackageSchemaFieldDto : BaseRequestPagingDto
    {
        [FromQuery(Name = "packageType")]
        public int PackageType { get; set; }

        [FromQuery(Name = "metadataType")]
        public int? MetadataType { get; set; }
    }
}
