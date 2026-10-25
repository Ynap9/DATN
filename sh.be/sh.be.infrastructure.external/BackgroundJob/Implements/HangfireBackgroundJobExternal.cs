using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base.Dtos.BackgroundJob;
using sh.be.applications.Base.Interfaces.External.BackgroundJob;
using sh.be.shared.Constants.BackgroundJob;
using sh.be.shared.HttpRequest.Exception;
using System.Linq.Expressions;

namespace sh.be.infrastructure.external.BackgroundJob.Implements
{
    public class HangfireBackgroundJobExternal : IBackgroundJobExternal
    {
        private static readonly IReadOnlyDictionary<string, int> StatusByStateName = new Dictionary<string, int>
        {
            { ProcessingState.StateName, BackgroundJobConst.STATUS_RUNNING },
            { SucceededState.StateName, BackgroundJobConst.STATUS_SUCCEEDED },
            { FailedState.StateName, BackgroundJobConst.STATUS_FAILED },
            { DeletedState.StateName, BackgroundJobConst.STATUS_FAILED },
        };

        private readonly JobStorage _jobStorage;
        private readonly IRecurringJobManagerV2 _recurringJobManager;
        private readonly IBackgroundJobClientV2 _backgroundJobClient;
        private readonly ILogger<HangfireBackgroundJobExternal> _logger;

        public HangfireBackgroundJobExternal(JobStorage jobStorage, IRecurringJobManagerV2 recurringJobManager, IBackgroundJobClientV2 backgroundJobClient, ILogger<HangfireBackgroundJobExternal> logger)
        {
            _jobStorage = jobStorage;
            _recurringJobManager = recurringJobManager;
            _backgroundJobClient = backgroundJobClient;
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

        public bool HasServerForQueue(string queue)
        {
            var aliveFrom = DateTime.UtcNow.AddSeconds(-BackgroundJobConst.SERVER_ALIVE_SECONDS);
            return _jobStorage.GetMonitoringApi().Servers().Any(server => server.Queues.Contains(queue) && server.Heartbeat > aliveFrom);
        }

        public string Enqueue<TJob>(Expression<Func<TJob, Task>> job, string queue, IReadOnlyDictionary<string, string> parameters)
        {
            _logger.LogInformation($"{nameof(Enqueue)}, job={typeof(TJob).Name}, queue={queue}");
            return _backgroundJobClient.Create(Job.FromExpression(job, queue), new EnqueuedState(queue),
                parameters.ToDictionary(parameter => parameter.Key, parameter => (object)parameter.Value));
        }

        public BackgroundJobDetailDto? FindJob(string jobId, IReadOnlyCollection<string> parameterNames)
        {
            using var connection = _jobStorage.GetConnection();
            var jobData = connection.GetJobData(jobId);
            if (jobData == null)
            {
                return null;
            }

            string? errorMessage = null;
            if (jobData.State == FailedState.StateName)
            {
                var state = connection.GetStateData(jobId);
                if (state != null
                    && state.Data.TryGetValue("ExceptionType", out var exceptionType)
                    && exceptionType == typeof(UserFriendlyException).FullName)
                {
                    state.Data.TryGetValue("ExceptionMessage", out errorMessage);
                }
            }

            return new BackgroundJobDetailDto
            {
                JobId = jobId,
                Status = jobData.State != null ? StatusByStateName.GetValueOrDefault(jobData.State, BackgroundJobConst.STATUS_WAITING) : BackgroundJobConst.STATUS_WAITING,
                ErrorMessage = errorMessage,
                Parameters = parameterNames.ToDictionary(name => name,
                    name => jobData.ParametersSnapshot != null && jobData.ParametersSnapshot.TryGetValue(name, out var value) ? SerializationHelper.Deserialize<string?>(value) : null),
            };
        }
    }
}
