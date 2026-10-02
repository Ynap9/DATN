using sh.be.applications.Module.Core.Dtos.AuditLog;
using sh.be.shared.HttpRequest.BaseRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Core.Interfaces
{
    public interface IAuditLogService
    {
        Task<BaseResponsePagingDto<ViewAuditLogDto>> FindPaging(FindPagingAuditLogDto dto);
        Task<FindAuditLogByIdDto> FindById(long id);
        Task Create(CreateAuditLogDto dto);
    }
}
