using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Operation.Dtos.Wage
{
    public class FindPagingWageDto : BaseRequestPagingDto
    {
        public int? DocumentTypeId { get; set; }
        public string? Role { get; set; }
    }
}
