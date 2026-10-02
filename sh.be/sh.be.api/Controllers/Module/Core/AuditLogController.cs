using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sh.be.api.Attributes;
using sh.be.api.Base;
using sh.be.applications.Module.Core.Dtos.AuditLog;
using sh.be.applications.Module.Core.Interfaces;
using sh.be.shared.Constants.Auth;
using sh.be.shared.HttpRequest;

namespace sh.be.api.Controllers.Module.Core
{
    [Route("api/core/audit-log")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AuditLogController : BaseController
    {
        private readonly IAuditLogService _auditLogService;
        public AuditLogController(ILogger<AuditLogController> logger, IAuditLogService auditLogService) : base(logger)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet("paging")]
        [Permission(PermissionConstants.FUNCTION_AUDIT_LOG_VIEW)]
        public async Task<ApiResponse> FindPaging([FromQuery] FindPagingAuditLogDto dto)
        {
            try
            {
                var data = await _auditLogService.FindPaging(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}")]
        [Permission(PermissionConstants.FUNCTION_AUDIT_LOG_VIEW)]
        public async Task<ApiResponse> FindById([FromRoute] long id)
        {
            try
            {
                var data = await _auditLogService.FindById(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
