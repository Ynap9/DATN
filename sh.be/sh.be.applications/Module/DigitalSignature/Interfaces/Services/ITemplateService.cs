using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Module.DigitalSignature.Dtos.Template;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.DigitalSignature.Interfaces.Services
{
    public interface ITemplateService
    {
        Task<BaseResponsePagingDto<GetListTemplateDto>> FindPaging(FindPagingTemplateDto dto);
        Task<FindTemplateByIdDto> FindById(int id);
        ExportFileDto GetSampleFile();
        Task<FindTemplateByIdDto> Create(CreateTemplateDto dto);
        Task Update(int id, UpdateTemplateDto dto);
        Task<FindTemplateByIdDto> UpdateConfig(int id, UpdateConfigTemplateDto dto);
        Task Delete(int id);
    }
}
