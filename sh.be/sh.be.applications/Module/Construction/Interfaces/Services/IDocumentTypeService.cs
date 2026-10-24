using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Module.Construction.Dtos.DocumentField;
using sh.be.applications.Module.Construction.Dtos.DocumentType;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Interfaces.Services
{
    public interface IDocumentTypeService
    {
        Task<List<GetListDocumentTypeDropDownDto>> GetDocumentTypeDropDown();
        Task<BaseResponsePagingDto<GetListDocumentTypeDto>> FindPaging(FindPagingDocumentTypeDto dto);
        Task<FindDocumentTypeByIdDto> FindById(int id);
        Task<List<GetListDocumentFieldDto>> GetListDocumentField(int id);
        Task<ExportFileDto> ExportExcelTemplate(int id);
        Task Create(CreateDocumentTypeDto dto);
        Task Update(int id, UpdateDocumentTypeDto dto);
        Task Delete(int id);
    }
}
