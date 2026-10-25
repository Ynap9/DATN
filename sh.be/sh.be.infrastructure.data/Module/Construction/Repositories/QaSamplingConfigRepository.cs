using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Construction.QaAssignments.Entities;
using sh.be.domain.Module.Construction.QaAssignments.Repositories;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Module.Construction.Repositories
{
    public class QaSamplingConfigRepository : IQaSamplingConfigRepository
    {
        private readonly ShDbContext _shDbContext;

        public QaSamplingConfigRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public Task<QaSamplingConfig?> FindByProjectAsync(int projectId)
        {
            return _shDbContext.QaSamplingConfigs.FirstOrDefaultAsync(c => c.ProjectId == projectId);
        }

        public void Add(QaSamplingConfig config)
        {
            _shDbContext.QaSamplingConfigs.Add(config);
        }

        public void Update(QaSamplingConfig config)
        {
            _shDbContext.QaSamplingConfigs.Update(config);
        }
    }
}
