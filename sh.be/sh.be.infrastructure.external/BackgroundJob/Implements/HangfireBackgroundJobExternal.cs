using Hangfire;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base.Dtos.BackgroundJob;
using sh.be.applications.Base.Interfaces.External.BackgroundJob;

namespace sh.be.infrastructure.external.BackgroundJob.Implements
{
    public class HangfireBackgroundJobExternal : IBackgroundJobExternal
    {
        private readonly JobStorage _jobStorage;
        private readonly ILogger<HangfireBackgroundJobExternal> _logger;

        public HangfireBackgroundJobExternal(JobStorage jobStorage, ILogger<HangfireBackgroundJobExternal> logger)
        {
            _jobStorage = jobStorage;
            _logger = logger;
        }

        public BackgroundJobConnectionDto CheckConnection()
        {
            try
            {
                return new BackgroundJobConnectionDto
                {
                    IsConnected = true,
                    ServerCount = _jobStorage.GetMonitoringApi().Servers().Count,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(CheckConnection)}");
                return new BackgroundJobConnectionDto
                {
                    IsConnected = false,
                    ServerCount = 0,
                };
            }
        }
    }
}
