using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Construction.Dtos.Project;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.infrastructure.data.Module.Construction.Queries
{
    public class ProjectQuery : BaseQuery, IProjectQuery
    {
        public ProjectQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        public Task<List<GetListProjectDropDownDto>> GetProjectDropDown()
        {
            return _shDbContext.Projects
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ProjectTo<GetListProjectDropDownDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<BaseResponsePagingDto<GetListProjectDto>> FindPaging(FindPagingProjectDto dto)
        {
            var query = _shDbContext.Projects.AsNoTracking();
            if (dto.Status.HasValue)
            {
                query = query.Where(p => p.Status == dto.Status);
            }
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(p => p.Name.Contains(dto.Keyword));
            }

            var totalCount = await query.CountAsync();

            var projects = await query
                .OrderByDescending(p => p.CreatedDate)
                .ThenByDescending(p => p.Id)
                .Paging(dto)
                .ProjectTo<GetListProjectDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return new BaseResponsePagingDto<GetListProjectDto>
            {
                Items = projects,
                TotalItems = totalCount,
            };
        }

        public Task<FindProjectByIdDto?> FindById(int id)
        {
            return _shDbContext.Projects
                .AsNoTracking()
                .Where(p => p.Id == id)
                .ProjectTo<FindProjectByIdDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }
    }
}
