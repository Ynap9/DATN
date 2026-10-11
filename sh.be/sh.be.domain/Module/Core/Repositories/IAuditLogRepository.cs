using sh.be.domain.Module.Core.Entities;

namespace sh.be.domain.Module.Core.Repositories
{
    public interface IAuditLogRepository
    {
        void Add(AuditLog auditLog);
    }
}
