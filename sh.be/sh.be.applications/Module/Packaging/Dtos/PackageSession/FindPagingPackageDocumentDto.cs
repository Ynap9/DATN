using Microsoft.AspNetCore.Mvc;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Packaging.Dtos.PackageSession
{
    public class FindPagingPackageDocumentDto : BaseRequestPagingDto
    {
        [FromQuery(Name = "dossierId")]
        public int? DossierId { get; set; }

        [FromQuery(Name = "status")]
        public string? Status { get; set; }
    }
}
