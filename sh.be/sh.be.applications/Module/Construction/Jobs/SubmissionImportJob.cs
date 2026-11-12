using Microsoft.Extensions.Logging;
using sh.be.applications.Module.Construction.Dtos.Submission;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;

namespace sh.be.applications.Module.Construction.Jobs
{
    public class SubmissionImportJob
    {
        private readonly ISubmissionService _submissionService;
        private readonly ILogger<SubmissionImportJob> _logger;

        public SubmissionImportJob(ISubmissionService submissionService, ILogger<SubmissionImportJob> logger)
        {
            _submissionService = submissionService;
            _logger = logger;
        }

        public Task<string> ExecuteAppendAsync(ImportExcelSubmissionDto dto, string userId)
        {
            return RunWithoutRetryAsync(nameof(ExecuteAppendAsync), dto, () => _submissionService.ImportExcelAppendFromJob(dto, userId));
        }

        public Task<string> ExecuteBlankAsync(ImportExcelSubmissionDto dto, string userId)
        {
            return RunWithoutRetryAsync(nameof(ExecuteBlankAsync), dto, () => _submissionService.ImportExcelBlankFromJob(dto, userId));
        }

        public Task<string> ExecuteOverwriteAsync(ImportExcelOverwriteSubmissionDto dto, string userId)
        {
            return RunWithoutRetryAsync(nameof(ExecuteOverwriteAsync), dto, () => _submissionService.ImportExcelOverwriteFromJob(dto, userId));
        }

        // Import không retry với mọi lỗi: lỗi dữ liệu thử lại vẫn sai, lỗi hạ tầng tự retry dễ ghi 2 lần.
        // Filter retry toàn cục của Hangfire bỏ qua UserFriendlyException nên bọc lỗi khác về loại này (FindJob cũng chỉ đọc message của loại này).
        private async Task<string> RunWithoutRetryAsync(string methodName, ImportExcelSubmissionDto dto, Func<Task<string>> importAsync)
        {
            _logger.LogInformation($"{methodName}, dto={JsonSerializer.Serialize(dto)}");
            try
            {
                return await importAsync();
            }
            catch (Exception ex) when (ex is not UserFriendlyException)
            {
                _logger.LogError(ex, $"{methodName}, fileKey={dto.FileKey}");
                throw new UserFriendlyException(ErrorCodes.InternalServerError, SubmissionConst.IMPORT_FAILED_MESSAGE);
            }
        }
    }
}
