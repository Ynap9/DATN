using sh.be.applications.Module.Construction.Dtos.DocumentField;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Interfaces.Queries
{
    public interface IDocumentFieldQuery
    {
        Task<List<GetListDocumentFieldDropDownDto>> GetDocumentFieldDropDown(int documentTypeId);        
        Task<BaseResponsePagingDto<GetListDocumentFieldDto>> FindPaging(FindPagingDocumentFieldDto dto);
        Task<FindDocumentFieldByIdDto?> FindById(int id);
        Task<List<FieldImportHeaderDto>> GetListFieldImport(int documentTypeId);
    }
}
