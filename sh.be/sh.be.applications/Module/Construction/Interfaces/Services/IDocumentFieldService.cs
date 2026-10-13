using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Module.Construction.Dtos.DocumentField;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Interfaces.Services
{
    public interface IDocumentFieldService
    {
        Task<List<GetListDocumentFieldDropDownDto>> GetDocumentFieldDropDown(int documentTypeId);        Task<BaseResponsePagingDto<GetListDocumentFieldDto>> FindPaging(FindPagingDocumentFieldDto dto);
        Task<FindDocumentFieldByIdDto> FindById(int id);
        Task<List<SheetHeaderDto>> GetSheetHeaders(GetSheetHeaderDto dto);
        Task Create(CreateDocumentFieldDto dto);
        Task QuickCreate(QuickCreateDocumentFieldDto dto);
        Task Update(int id, UpdateDocumentFieldDto dto);
        Task UpdateSetting(int id, UpdateSettingDocumentFieldDto dto);
        Task ChangeOrderTableView(int id, ChangeOrderDocumentFieldDto dto);
        Task ChangeOrderListView(int id, ChangeOrderDocumentFieldDto dto);
        Task Delete(int id);
        Task QuickDelete(QuickDeleteDocumentFieldDto dto);
    }
}
