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
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.applications.Module.Construction.Implements
{
    public class SubmissionFileService : BaseService, ISubmissionFileService
    {
        private readonly SubmissionDomainService _submissionDomainService;
        private readonly IFileDownloadExternal _fileDownloadExternal;
        private readonly ISubmissionAssignmentService _submissionAssignmentService;

        public SubmissionFileService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<SubmissionFileService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            SubmissionDomainService submissionDomainService, IFileDownloadExternal fileDownloadExternal, ISubmissionAssignmentService submissionAssignmentService
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _submissionDomainService = submissionDomainService;
            _fileDownloadExternal = fileDownloadExternal;
            _submissionAssignmentService = submissionAssignmentService;
        }

        // Mở PDF của văn bản khi người gọi được xem ít nhất một văn bản gắn file đó
        public async Task<FileStreamDto> OpenPdf(string fileUrl, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(OpenPdf)}, fileUrl={fileUrl}");
            var submissions = await _submissionDomainService.EnsureFileUrlExistsAsync(fileUrl);
            var currentUserId = getCurrentUserId();

            // Luật xem như chi tiết văn bản; qua một văn bản là đủ, không qua văn bản nào thì ném lỗi của văn bản đầu tiên
            UserFriendlyException? firstError = null;
            foreach (var submission in submissions)
            {
                try
                {
                    await _submissionAssignmentService.EnsureCanViewAsync(submission, currentUserId);
                    firstError = null;
                    break;
                }
                catch (UserFriendlyException ex)
                {
                    firstError ??= ex;
                }
            }
            if (firstError != null)
            {
                throw firstError;
            }
            return await _fileDownloadExternal.OpenPdfAsync(fileUrl, cancellationToken);
        }
    }
}
