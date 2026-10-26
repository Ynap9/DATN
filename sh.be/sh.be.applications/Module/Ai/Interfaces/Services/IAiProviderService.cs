using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Module.Ai.Dtos.AiProvider;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Ai.Interfaces.Services
{
    public interface IAiProviderService
    {
        Task<BaseResponsePagingDto<GetListAiProviderDto>> FindPaging(FindPagingAiProviderDto dto);
        Task<List<GetListAiProviderDropDownDto>> GetDropDown();
        Task<FindAiProviderByIdDto> FindById(int id);
        Task<int> Create(CreateAiProviderDto dto);
        Task Update(int id, UpdateAiProviderDto dto);
        Task<ViewAiProviderApiKeyResultDto> UpdateApiKey(int id, UpdateAiProviderApiKeyDto dto);
        Task Delete(int id);
        Task<ExportFileDto> DownloadApiKey(int id);
    }
}
