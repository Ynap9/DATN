using Microsoft.Extensions.Options;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces.External.BackgroundJob;
using sh.be.applications.Module.Construction.Jobs;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Settings;

namespace sh.be.api.Worker
{
    public class RecurringJobRegistrar : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly BackgroundJobSettings _backgroundJobSettings;

        public RecurringJobRegistrar(IServiceProvider serviceProvider, IOptions<BackgroundJobSettings> backgroundJobSettings)
        {
            _serviceProvider = serviceProvider;
            _backgroundJobSettings = backgroundJobSettings.Value;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_backgroundJobSettings.Enabled)
            {
                return Task.CompletedTask;
            }

            using var scope = _serviceProvider.CreateScope();
            var backgroundJobExternal = scope.ServiceProvider.GetRequiredService<IBackgroundJobExternal>();
            backgroundJobExternal.AddOrUpdateRecurring<ProjectDeadlineJob>(ProjectDeadlineConst.RECURRING_JOB_ID,
                job => job.ExecuteAsync(), _backgroundJobSettings.Jobs.ProjectDeadlineCheck, BaseService.VietnamTimeZone);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
