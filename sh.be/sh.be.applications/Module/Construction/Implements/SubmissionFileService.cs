using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.Storage;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Base.Interfaces.External.Storage;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Construction.Submissions.Services;
using sh.be.domain.Module.Core.Repositories;

namespace sh.be.applications.Module.Construction.Implements
{
    public class SubmissionFileService : BaseService, ISubmissionFileService
    {
        private readonly SubmissionDomainService _submissionDomainService;
        private readonly IFileDownloadExternal _fileDownloadExternal;

        public SubmissionFileService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<SubmissionFileService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            SubmissionDomainService submissionDomainService, IFileDownloadExternal fileDownloadExternal
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _submissionDomainService = submissionDomainService;
            _fileDownloadExternal = fileDownloadExternal;
        }

        public async Task<FileStreamDto> OpenPdf(string fileUrl, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(OpenPdf)}, fileUrl={fileUrl}");
            await _submissionDomainService.EnsureFileUrlExistsAsync(fileUrl);
            return await _fileDownloadExternal.OpenPdfAsync(fileUrl, cancellationToken);
        }
    }
}
