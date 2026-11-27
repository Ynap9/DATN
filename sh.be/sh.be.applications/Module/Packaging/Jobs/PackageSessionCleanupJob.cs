using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Base.Interfaces.External.Storage;
using sh.be.domain.Base;
using sh.be.domain.Module.Core.Repositories;
using sh.be.domain.Module.Packaging.Repositories;
using sh.be.domain.Module.Packaging.Services;
using sh.be.shared.Constants.BackgroundJob;

namespace sh.be.applications.Module.Packaging.Jobs
{
    public class PackageSessionCleanupJob : BaseService
    {
        private readonly IPackageSessionRepository _packageSessionRepository;
        private readonly IFileStorageExternal _fileStorageExternal;

        public PackageSessionCleanupJob(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<PackageSessionCleanupJob> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IPackageSessionRepository packageSessionRepository, IFileStorageExternal fileStorageExternal
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _packageSessionRepository = packageSessionRepository;
            _fileStorageExternal = fileStorageExternal;
        }

        public async Task ExecuteAsync()
        {
            var now = GetVietnamTime();
            _logger.LogInformation($"{nameof(ExecuteAsync)}, now={now:yyyyMMddHHmm}");
            foreach (var session in await _packageSessionRepository.GetExpiredAsync(now))
            {
                await _fileStorageExternal.DeleteByPrefixAsync(PackageSessionDomainService.BuildSessionObjectKeyPrefix(session.Id));
                foreach (var dossier in await _packageSessionRepository.GetDossiersAsync(session.Id))
                {
                    dossier.Delete(BackgroundJobConst.SYSTEM_USER, now);
                }
                foreach (var document in await _packageSessionRepository.GetDocumentsAsync(session.Id))
                {
                    document.Delete(BackgroundJobConst.SYSTEM_USER, now);
                }
                session.Delete(BackgroundJobConst.SYSTEM_USER, now);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
