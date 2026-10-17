using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Dtos.DocumentField
{
    public class FindPagingDocumentFieldDto : BaseRequestPagingDto
    {
        public int? DocumentTypeId { get; set; }
    }
}
