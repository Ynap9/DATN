using sh.be.applications.Base.Dtos.BackgroundJob;
using System.Linq.Expressions;

namespace sh.be.applications.Base.Interfaces.External.BackgroundJob
{
    public interface IBackgroundJobExternal
    {
        BackgroundJobConnectionDto CheckConnection();
        void AddOrUpdateRecurring<TJob>(string recurringJobId, Expression<Func<TJob, Task>> job, string cron, TimeZoneInfo timeZone);
        string? TriggerRecurring(string recurringJobId);
        RecurringJobInfoDto? FindRecurring(string recurringJobId);
        bool HasServerForQueue(string queue);
        string Enqueue<TJob>(Expression<Func<TJob, Task>> job, string queue, IReadOnlyDictionary<string, string> parameters);
        BackgroundJobDetailDto? FindJob(string jobId, IReadOnlyCollection<string> parameterNames);
    }
}
