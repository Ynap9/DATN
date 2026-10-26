using sh.be.applications.Module.Ai.Dtos.AiProvider;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Ai.Interfaces.Queries
{
    public interface IAiProviderQuery
    {
        Task<BaseResponsePagingDto<GetListAiProviderDto>> FindPaging(FindPagingAiProviderDto dto);
        Task<List<GetListAiProviderDropDownDto>> GetDropDown();
        Task<FindAiProviderByIdDto?> FindById(int id);
    }
}
