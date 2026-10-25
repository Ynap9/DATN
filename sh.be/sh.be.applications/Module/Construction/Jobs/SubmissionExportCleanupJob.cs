using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Base.Interfaces.External.Storage;
using sh.be.domain.Base;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.Construction;

namespace sh.be.applications.Module.Construction.Jobs
{
    public class SubmissionExportCleanupJob : BaseService
    {
        private readonly IFileStorageExternal _fileStorageExternal;

        public SubmissionExportCleanupJob(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<SubmissionExportCleanupJob> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IFileStorageExternal fileStorageExternal
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _fileStorageExternal = fileStorageExternal;
        }

        public async Task ExecuteAsync()
        {
            var today = GetVietnamTime().Date;
            _logger.LogInformation($"{nameof(ExecuteAsync)}, today={today:yyyyMMdd}");
            for (var day = SubmissionConst.EXPORT_FILE_RETENTION_DAYS + 1; day <= SubmissionConst.EXPORT_FILE_RETENTION_DAYS + SubmissionConst.EXPORT_CLEANUP_LOOKBACK_DAYS; day++)
            {
                await _fileStorageExternal.DeleteByPrefixAsync($"{SubmissionConst.EXPORT_OBJECT_KEY_PREFIX}/{today.AddDays(-day):yyyyMMdd}/");
            }
        }
    }
}
