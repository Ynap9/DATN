using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base.Dtos.BackgroundJob;
using sh.be.applications.Base.Interfaces.External.BackgroundJob;
using System.Linq.Expressions;

namespace sh.be.infrastructure.external.BackgroundJob.Implements
{
    public class HangfireBackgroundJobExternal : IBackgroundJobExternal
    {
        private readonly JobStorage _jobStorage;
        private readonly IRecurringJobManagerV2 _recurringJobManager;
        private readonly ILogger<HangfireBackgroundJobExternal> _logger;

        public HangfireBackgroundJobExternal(JobStorage jobStorage, IRecurringJobManagerV2 recurringJobManager, ILogger<HangfireBackgroundJobExternal> logger)
        {
            _jobStorage = jobStorage;
            _recurringJobManager = recurringJobManager;
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

        public void AddOrUpdateRecurring<TJob>(string recurringJobId, Expression<Func<TJob, Task>> job, string cron, TimeZoneInfo timeZone)
        {
            _logger.LogInformation($"{nameof(AddOrUpdateRecurring)}, recurringJobId={recurringJobId}, cron={cron}, timeZone={timeZone.Id}");
            _recurringJobManager.AddOrUpdate(recurringJobId, job, cron, new RecurringJobOptions { TimeZone = timeZone });
        }

        public string? TriggerRecurring(string recurringJobId)
        {
            _logger.LogInformation($"{nameof(TriggerRecurring)}, recurringJobId={recurringJobId}");
            return _recurringJobManager.TriggerJob(recurringJobId);
        }

        public RecurringJobInfoDto? FindRecurring(string recurringJobId)
        {
            using var connection = _jobStorage.GetConnection();
            var recurringJob = connection.GetRecurringJobs([recurringJobId]).FirstOrDefault();
            if (recurringJob == null || recurringJob.Removed)
            {
                return null;
            }

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(recurringJob.TimeZoneId ?? TimeZoneInfo.Utc.Id);
            return new RecurringJobInfoDto
            {
                Id = recurringJob.Id,
                Cron = recurringJob.Cron,
                TimeZoneId = recurringJob.TimeZoneId,
                NextExecution = recurringJob.NextExecution.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(recurringJob.NextExecution.Value, timeZone) : null,
                LastExecution = recurringJob.LastExecution.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(recurringJob.LastExecution.Value, timeZone) : null,
                LastJobId = recurringJob.LastJobId,
                LastJobState = recurringJob.LastJobState,
                Error = recurringJob.Error,
            };
        }
    }
}
