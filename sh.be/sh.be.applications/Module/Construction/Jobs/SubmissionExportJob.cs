using Microsoft.Extensions.Logging;
using sh.be.applications.Base.Interfaces.External.Storage;
using sh.be.applications.Module.Construction.Dtos.Submission;
using sh.be.applications.Module.Construction.Interfaces.Services;
using System.Text.Json;

namespace sh.be.applications.Module.Construction.Jobs
{
    public class SubmissionExportJob
    {
        private readonly ISubmissionService _submissionService;
        private readonly IFileStorageExternal _fileStorageExternal;
        private readonly ILogger<SubmissionExportJob> _logger;

        public SubmissionExportJob(ISubmissionService submissionService, IFileStorageExternal fileStorageExternal, ILogger<SubmissionExportJob> logger)
        {
            _submissionService = submissionService;
            _fileStorageExternal = fileStorageExternal;
            _logger = logger;
        }

        public async Task ExecuteAsync(ExportSubmissionDto dto, string objectKey)
        {
            _logger.LogInformation($"{nameof(ExecuteAsync)}, dto={JsonSerializer.Serialize(dto)}, objectKey={objectKey}");
            var file = await _submissionService.ExportExcel(dto);
            using var content = new MemoryStream(file.Content);
            await _fileStorageExternal.UploadPrivateAsync(content, objectKey, file.ContentType);
        }
    }
}
