using sh.be.applications.Module.Construction.Dtos.DocumentType;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Interfaces.Queries
{
    public interface IDocumentTypeQuery
    {
        Task<List<GetListDocumentTypeDropDownDto>> GetDocumentTypeDropDown();
        Task<BaseResponsePagingDto<GetListDocumentTypeDto>> FindPaging(FindPagingDocumentTypeDto dto);
        Task<FindDocumentTypeByIdDto?> FindById(int id);
    }
}
