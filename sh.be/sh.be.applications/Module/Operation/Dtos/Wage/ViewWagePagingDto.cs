using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Operation.Dtos.Wage
{
    public class ViewWagePagingDto : BaseResponsePagingDto<GetListWageDto>
    {
        public ViewWageSummaryDto Summary { get; set; } = new();
    }
}
