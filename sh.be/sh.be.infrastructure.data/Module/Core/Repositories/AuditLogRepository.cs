using sh.be.domain.Module.Core.Entities;
using sh.be.domain.Module.Core.Repositories;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Module.Core.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ShDbContext _shDbContext;

        public AuditLogRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public void Add(AuditLog auditLog)
        {
            _shDbContext.AuditLogs.Add(auditLog);
        }
    }
}
