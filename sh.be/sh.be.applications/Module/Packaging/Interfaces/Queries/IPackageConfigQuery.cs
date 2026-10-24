using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Module.Packaging.Dtos.PackageConfig;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Packaging.Interfaces.Queries
{
    public interface IPackageConfigQuery
    {
        Task<List<GetListPackageSchemaDropDownDto>> GetSchemaDropDown();
        Task<BaseResponsePagingDto<GetListPackageSchemaFieldDto>> FindFieldPaging(FindPagingPackageSchemaFieldDto dto);
        Task<List<ExcelSheetTemplateDto>> GetTemplateSheets(int packageType, IReadOnlyCollection<int> metadataTypes);
    }
}
