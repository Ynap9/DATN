using Microsoft.Extensions.Logging;
using sh.be.applications.Module.Packaging.Interfaces.Services;
using sh.be.shared.Constants.Packaging;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.applications.Module.Packaging.Jobs
{
    public class PackageBuildJob
    {
        private readonly IPackageBuildService _packageBuildService;
        private readonly ILogger<PackageBuildJob> _logger;

        public PackageBuildJob(IPackageBuildService packageBuildService, ILogger<PackageBuildJob> logger)
        {
            _packageBuildService = packageBuildService;
            _logger = logger;
        }

        public async Task ExecuteAsync(int sessionId, string userId)
        {
            _logger.LogInformation($"{nameof(ExecuteAsync)}, sessionId={sessionId}, userId={userId}");
            try
            {
                await _packageBuildService.BuildFromJob(sessionId, userId);
            }
            catch (Exception ex) when (ex is not UserFriendlyException)
            {
                _logger.LogError(ex, $"{nameof(ExecuteAsync)}, sessionId={sessionId}");
                throw new UserFriendlyException(ErrorCodes.InternalServerError, PackageBuildConst.BUILD_FAILED_MESSAGE);
            }
        }
    }
}
