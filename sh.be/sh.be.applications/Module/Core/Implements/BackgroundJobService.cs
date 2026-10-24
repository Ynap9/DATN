using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.BackgroundJob;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Base.Interfaces.External.BackgroundJob;
using sh.be.applications.Module.Core.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Core.Repositories;

namespace sh.be.applications.Module.Core.Implements
{
    public class BackgroundJobService : BaseService, IBackgroundJobService
    {
        private readonly IBackgroundJobExternal _backgroundJobExternal;

        public BackgroundJobService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<BackgroundJobService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IBackgroundJobExternal backgroundJobExternal
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _backgroundJobExternal = backgroundJobExternal;
        }

        public BackgroundJobConnectionDto CheckConnection()
        {
            _logger.LogInformation($"{nameof(CheckConnection)}");
            return _backgroundJobExternal.CheckConnection();
        }
    }
}
