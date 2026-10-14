using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Construction.Dtos.DocumentField;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.infrastructure.data.Module.Construction.Queries
{
    public class DocumentFieldQuery : BaseQuery, IDocumentFieldQuery
    {
        public DocumentFieldQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        public Task<List<GetListDocumentFieldDropDownDto>> GetDocumentFieldDropDown(int documentTypeId)
        {
            return _shDbContext.DocumentFields
                .AsNoTracking()
                .Where(d => d.DocumentTypeId == documentTypeId)
                .OrderBy(d => d.OrderTableView)
                .ThenBy(d => d.Id)
                .ProjectTo<GetListDocumentFieldDropDownDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<BaseResponsePagingDto<GetListDocumentFieldDto>> FindPaging(FindPagingDocumentFieldDto dto)
        {
            var query = _shDbContext.DocumentFields.AsNoTracking();
            if (dto.DocumentTypeId.HasValue)
            {
                query = query.Where(d => d.DocumentTypeId == dto.DocumentTypeId);
            }
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(d => d.Name.Contains(dto.Keyword) || d.Field.Contains(dto.Keyword));
            }

            var totalCount = await query.CountAsync();

            var documentFields = await query
                .OrderBy(d => d.DocumentTypeId)
                .ThenBy(d => d.OrderTableView)
                .ThenBy(d => d.Id)
                .Paging(dto)
                .ProjectTo<GetListDocumentFieldDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return new BaseResponsePagingDto<GetListDocumentFieldDto>
            {
                Items = documentFields,
                TotalItems = totalCount,
            };
        }

        public Task<FindDocumentFieldByIdDto?> FindById(int id)
        {
            return _shDbContext.DocumentFields
                .AsNoTracking()
                .Where(d => d.Id == id)
                .ProjectTo<FindDocumentFieldByIdDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }

        public Task<List<FieldImportHeaderDto>> GetListFieldImport(int documentTypeId)
        {
            return _shDbContext.DocumentFields
                .AsNoTracking()
                .Where(d => d.DocumentTypeId == documentTypeId)
                .OrderBy(d => d.OrderTableView)
                .ThenBy(d => d.Id)
                .ProjectTo<FieldImportHeaderDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }
    }
}
