using sh.be.applications.Module.Core.Dtos.AuditLog;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Core.Interfaces.Queries
{
    public interface IAuditLogQuery
    {
        Task<BaseResponsePagingDto<ViewAuditLogDto>> FindPaging(FindPagingAuditLogDto dto);
        Task<FindAuditLogByIdDto?> FindById(long id);
    }
}
