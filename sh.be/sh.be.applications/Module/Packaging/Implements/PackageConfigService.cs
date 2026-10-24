using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Base.Interfaces.External.DataReader;
using sh.be.applications.Module.Packaging.Dtos.PackageConfig;
using sh.be.applications.Module.Packaging.Interfaces.Queries;
using sh.be.applications.Module.Packaging.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Core.Repositories;
using sh.be.domain.Module.Packaging.Services;
using sh.be.shared.Constants.Packaging;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;

namespace sh.be.applications.Module.Packaging.Implements
{
    public class PackageConfigService : BaseService, IPackageConfigService
    {
        private readonly IPackageConfigQuery _packageConfigQuery;
        private readonly IExcelExternal _excelExternal;

        public PackageConfigService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<PackageConfigService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IPackageConfigQuery packageConfigQuery, IExcelExternal excelExternal
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _packageConfigQuery = packageConfigQuery;
            _excelExternal = excelExternal;
        }

        public async Task<List<GetListPackageTypeDropDownDto>> GetPackageTypeDropDown()
        {
            _logger.LogInformation($"{nameof(GetPackageTypeDropDown)}");
            var schemas = await _packageConfigQuery.GetSchemaDropDown();
            return PackageConst.PACKAGE_TYPES
                .Select(packageType => new GetListPackageTypeDropDownDto
                {
                    PackageType = packageType,
                    Code = PackageConst.PACKAGE_TYPE_CODES[packageType],
                    Name = PackageConst.PACKAGE_TYPE_NAMES[packageType],
                    Schemas = schemas.Where(s => s.PackageType == packageType).ToList(),
                })
                .Where(p => p.Schemas.Count > 0)
                .ToList();
        }

        public async Task<BaseResponsePagingDto<GetListPackageSchemaFieldDto>> FindFieldPaging(FindPagingPackageSchemaFieldDto dto)
        {
            _logger.LogInformation($"{nameof(FindFieldPaging)}, dto={JsonSerializer.Serialize(dto)}");
            PackageSchemaPolicy.EnsureValidPackageType(dto.PackageType);
            PackageSchemaPolicy.EnsureValidMetadataTypes(dto.MetadataType.HasValue ? [dto.MetadataType.Value] : []);
            return await _packageConfigQuery.FindFieldPaging(dto);
        }

        public async Task<ExportFileDto> ExportExcelTemplate(ExportPackageExcelTemplateDto dto)
        {
            _logger.LogInformation($"{nameof(ExportExcelTemplate)}, dto={JsonSerializer.Serialize(dto)}");
            PackageSchemaPolicy.EnsureValidPackageType(dto.PackageType);
            PackageSchemaPolicy.EnsureValidMetadataTypes(dto.MetadataTypes);

            var sheets = await _packageConfigQuery.GetTemplateSheets(dto.PackageType, dto.MetadataTypes.Distinct().ToList());
            if (sheets.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.PackagingErrorSchemaFieldNotFound);
            }
            return _excelExternal.CreateMultiSheetTemplate($"Mau metadata {PackageConst.PACKAGE_TYPE_CODES[dto.PackageType]}", sheets);
        }
    }
}
