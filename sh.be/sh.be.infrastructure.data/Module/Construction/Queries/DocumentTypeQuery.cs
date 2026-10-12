using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Construction.Dtos.DocumentType;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.infrastructure.data.Module.Construction.Queries
{
    public class DocumentTypeQuery : BaseQuery, IDocumentTypeQuery
    {
        public DocumentTypeQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        public Task<List<GetListDocumentTypeDropDownDto>> GetDocumentTypeDropDown()
        {
            return _shDbContext.DocumentTypes
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .ProjectTo<GetListDocumentTypeDropDownDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<BaseResponsePagingDto<GetListDocumentTypeDto>> FindPaging(FindPagingDocumentTypeDto dto)
        {
            var query = _shDbContext.DocumentTypes.AsNoTracking();
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(d => d.Name.Contains(dto.Keyword));
            }

            var totalCount = await query.CountAsync();

            var documentTypes = await query
                .OrderByDescending(d => d.CreatedDate)
                .ThenByDescending(d => d.Id)
                .Paging(dto)
                .ProjectTo<GetListDocumentTypeDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return new BaseResponsePagingDto<GetListDocumentTypeDto>
            {
                Items = documentTypes,
                TotalItems = totalCount,
            };
        }

        public Task<FindDocumentTypeByIdDto?> FindById(int id)
        {
            return _shDbContext.DocumentTypes
                .AsNoTracking()
                .Where(d => d.Id == id)
                .ProjectTo<FindDocumentTypeByIdDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }
    }
}
