using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.File;
using sh.be.applications.Base.Interfaces;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Construction.Services;
using sh.be.domain.Module.Core.Repositories;

namespace sh.be.applications.Module.Construction.Implements
{
    public class SubmissionFileService : BaseService, ISubmissionFileService
    {
        private readonly SubmissionDomainService _submissionDomainService;
        private readonly IFileDownloader _fileDownloader;

        public SubmissionFileService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<SubmissionFileService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            SubmissionDomainService submissionDomainService, IFileDownloader fileDownloader
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _submissionDomainService = submissionDomainService;
            _fileDownloader = fileDownloader;
        }

        public async Task<FileStreamDto> OpenPdf(string fileUrl, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(OpenPdf)}, fileUrl={fileUrl}");
            await _submissionDomainService.EnsureFileUrlExistsAsync(fileUrl);
            return await _fileDownloader.OpenPdfAsync(fileUrl, cancellationToken);
        }
    }
}
