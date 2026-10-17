using sh.be.applications.Module.Construction.Dtos.DocumentField;
using sh.be.applications.Module.Construction.Dtos.DocumentType;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Interfaces.Queries
{
    public interface IDocumentTypeQuery
    {
        Task<List<GetListDocumentTypeDropDownDto>> GetDocumentTypeDropDown();
        Task<BaseResponsePagingDto<GetListDocumentTypeDto>> FindPaging(FindPagingDocumentTypeDto dto);
        Task<FindDocumentTypeByIdDto?> FindById(int id);
        Task<List<GetListDocumentFieldDto>> GetListDocumentField(int id);
        Task<List<string>> GetListTemplateHeader(int id);
    }
}
