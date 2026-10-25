using AutoMapper;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Module.Packaging.Dtos.PackageConfig;
using sh.be.applications.Module.Packaging.Interfaces.Queries;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.infrastructure.data.Module.Packaging.Queries
{
    public class PackageConfigQuery : BaseQuery, IPackageConfigQuery
    {
        public PackageConfigQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        public Task<List<GetListPackageSchemaDropDownDto>> GetSchemaDropDown()
        {
            return _shDbContext.PackageSchemas
                .AsNoTracking()
                .OrderBy(s => s.PackageType)
                .ThenBy(s => s.MetadataType)
                .Select(s => new GetListPackageSchemaDropDownDto
                {
                    PackageSchemaId = s.Id,
                    PackageType = s.PackageType,
                    MetadataType = s.MetadataType,
                    Name = s.Name,
                })
                .ToListAsync();
        }

        public async Task<BaseResponsePagingDto<GetListPackageSchemaFieldDto>> FindFieldPaging(FindPagingPackageSchemaFieldDto dto)
        {
            var query = from field in _shDbContext.PackageSchemaFields.AsNoTracking()
                        join schema in _shDbContext.PackageSchemas.AsNoTracking() on field.PackageSchemaId equals schema.Id
                        where schema.PackageType == dto.PackageType
                        select new { field, schema };
            if (dto.MetadataType.HasValue)
            {
                query = query.Where(x => x.schema.MetadataType == dto.MetadataType.Value);
            }
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(x => x.field.DisplayName.Contains(dto.Keyword) || x.field.ElementName.Contains(dto.Keyword));
            }

            var totalCount = await query.CountAsync();

            var fields = await query
                .OrderBy(x => x.schema.MetadataType)
                .ThenBy(x => x.field.SortOrder)
                .ThenBy(x => x.field.Id)
                .Paging(dto)
                .Select(x => new GetListPackageSchemaFieldDto
                {
                    Id = x.field.Id,
                    PackageSchemaId = x.field.PackageSchemaId,
                    PackageType = x.schema.PackageType,
                    MetadataType = x.schema.MetadataType,
                    SchemaName = x.schema.Name,
                    ElementName = x.field.ElementName,
                    DisplayName = x.field.DisplayName,
                    DataType = x.field.DataType,
                    MaxLength = x.field.MaxLength,
                    Requirement = x.field.Requirement,
                    ConditionElement = x.field.ConditionElement,
                    ConditionValues = x.field.ConditionValues,
                    CodeGroup = x.field.CodeGroup,
                    AllowMultipleValues = x.field.AllowMultipleValues,
                    Description = x.field.Description,
                    SortOrder = x.field.SortOrder,
                })
                .ToListAsync();

            var codeGroups = fields
                .Where(f => f.CodeGroup != null)
                .Select(f => f.CodeGroup!)
                .Distinct()
                .ToList();
            if (codeGroups.Count > 0)
            {
                var codeValues = await _shDbContext.PackageCodeValues
                    .AsNoTracking()
                    .Where(c => codeGroups.Contains(c.CodeGroup))
                    .OrderBy(c => c.CodeGroup)
                    .ThenBy(c => c.SortOrder)
                    .ThenBy(c => c.Code)
                    .Select(c => new { c.CodeGroup, c.Code, c.Name })
                    .ToListAsync();
                var codesByGroup = codeValues.ToLookup(c => c.CodeGroup, c => new GetListPackageCodeValueDto { Code = c.Code, Name = c.Name });
                foreach (var field in fields.Where(f => f.CodeGroup != null))
                {
                    field.Codes = codesByGroup[field.CodeGroup!].ToList();
                }
            }

            return new BaseResponsePagingDto<GetListPackageSchemaFieldDto>
            {
                Items = fields,
                TotalItems = totalCount,
            };
        }

        public async Task<List<ExcelSheetTemplateDto>> GetTemplateSheets(int packageType, IReadOnlyCollection<int> metadataTypes)
        {
            var schemas = _shDbContext.PackageSchemas
                .AsNoTracking()
                .Where(s => s.PackageType == packageType);
            if (metadataTypes.Count > 0)
            {
                schemas = schemas.Where(s => metadataTypes.Contains(s.MetadataType));
            }

            var headers = await (from field in _shDbContext.PackageSchemaFields.AsNoTracking()
                                 join schema in schemas on field.PackageSchemaId equals schema.Id
                                 orderby schema.MetadataType, field.SortOrder, field.Id
                                 select new { schema.MetadataType, SchemaName = schema.Name, field.DisplayName })
                .ToListAsync();

            return headers
                .GroupBy(h => new { h.MetadataType, h.SchemaName })
                .Select(g => new ExcelSheetTemplateDto
                {
                    SheetName = g.Key.SchemaName,
                    Headers = g.Select(h => h.DisplayName).ToList(),
                })
                .ToList();
        }
    }
}
