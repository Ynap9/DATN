using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Module.Packaging.Dtos.PackageConfig;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Packaging.Interfaces.Services
{
    public interface IPackageConfigService
    {
        Task<List<GetListPackageTypeDropDownDto>> GetPackageTypeDropDown();
        Task<BaseResponsePagingDto<GetListPackageSchemaFieldDto>> FindFieldPaging(FindPagingPackageSchemaFieldDto dto);
        Task<ExportFileDto> ExportExcelTemplate(ExportPackageExcelTemplateDto dto);
    }
}
