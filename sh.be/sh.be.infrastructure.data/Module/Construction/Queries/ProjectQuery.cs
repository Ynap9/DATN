using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Construction.Dtos.Project;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Module.Construction.Queries
{
    public class ProjectQuery : BaseQuery, IProjectQuery
    {
        public ProjectQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
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
