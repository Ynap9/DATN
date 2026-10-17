using sh.be.applications.Module.DigitalSignature.Dtos.Template;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.DigitalSignature.Interfaces.Queries
{
    public interface ITemplateQuery
    {
        Task<BaseResponsePagingDto<GetListTemplateDto>> FindPaging(FindPagingTemplateDto dto);
        Task<FindTemplateByIdDto?> FindById(int id);
    }
}
