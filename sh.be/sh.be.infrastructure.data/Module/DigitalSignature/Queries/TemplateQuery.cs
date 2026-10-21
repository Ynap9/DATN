using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.DigitalSignature.Dtos.Template;
using sh.be.applications.Module.DigitalSignature.Interfaces.Queries;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.infrastructure.data.Module.DigitalSignature.Queries
{
    public class TemplateQuery : BaseQuery, ITemplateQuery
    {
        public TemplateQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        public async Task<BaseResponsePagingDto<GetListTemplateDto>> FindPaging(FindPagingTemplateDto dto)
        {
            var query = _shDbContext.Templates.AsNoTracking();
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(t => t.Name.Contains(dto.Keyword));
            }

            var totalCount = await query.CountAsync();

            var templates = await query
                .OrderByDescending(t => t.CreatedDate)
                .ThenByDescending(t => t.Id)
                .Paging(dto)
                .ProjectTo<GetListTemplateDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return new BaseResponsePagingDto<GetListTemplateDto>
            {
                Items = templates,
                TotalItems = totalCount,
            };
        }

        public async Task<FindTemplateByIdDto?> FindById(int id)
        {
            var template = await _shDbContext.Templates
                .AsNoTracking()
                .Where(t => t.Id == id)
                .ProjectTo<FindTemplateByIdDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
            if (template == null)
            {
                return null;
            }

            template.Positions = await _shDbContext.TemplatePositions
                .AsNoTracking()
                .Where(tp => tp.TemplateId == id)
                .OrderBy(tp => tp.Kind)
                .ThenBy(tp => tp.Id)
                .ProjectTo<TemplatePositionDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
            return template;
        }
    }
}
