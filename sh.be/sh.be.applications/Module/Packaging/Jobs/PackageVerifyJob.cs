using Microsoft.Extensions.Logging;
using sh.be.applications.Module.Packaging.Interfaces.Services;
using sh.be.shared.Constants.Packaging;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.applications.Module.Packaging.Jobs
{
    public class PackageVerifyJob
    {
        private readonly IPackageSessionService _packageSessionService;
        private readonly ILogger<PackageVerifyJob> _logger;

        public PackageVerifyJob(IPackageSessionService packageSessionService, ILogger<PackageVerifyJob> logger)
        {
            _packageSessionService = packageSessionService;
            _logger = logger;
        }

        public async Task ExecuteAsync(int sessionId, string userId)
        {
            _logger.LogInformation($"{nameof(ExecuteAsync)}, sessionId={sessionId}, userId={userId}");
            try
            {
                await _packageSessionService.VerifyFromJob(sessionId, userId);
            }
            catch (Exception ex) when (ex is not UserFriendlyException)
            {
                _logger.LogError(ex, $"{nameof(ExecuteAsync)}, sessionId={sessionId}");
                throw new UserFriendlyException(ErrorCodes.InternalServerError, PackageSessionConst.VERIFY_FAILED_MESSAGE);
            }
        }
    }
}
