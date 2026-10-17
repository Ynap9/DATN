using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Construction.Dtos.ProjectConfig;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Module.Construction.Queries
{
    public class ProjectConfigQuery : BaseQuery, IProjectConfigQuery
    {
        public ProjectConfigQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        public Task<List<GetListProjectConfigDto>> GetList()
        {
            return _shDbContext.ProjectConfigs
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ProjectTo<GetListProjectConfigDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public Task<FindProjectConfigByIdDto?> FindById(int id)
        {
            return _shDbContext.ProjectConfigs
                .AsNoTracking()
                .Where(p => p.Id == id)
                .ProjectTo<FindProjectConfigByIdDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }
    }
}
