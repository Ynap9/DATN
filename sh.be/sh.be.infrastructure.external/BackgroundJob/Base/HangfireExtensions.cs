using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using sh.be.shared.Constants.BackgroundJob;
using sh.be.shared.Constants.Db;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Settings;

namespace sh.be.infrastructure.external.BackgroundJob.Base
{
    public static class HangfireExtensions
    {
        public static IServiceCollection AddHangfireBackgroundJob(this IServiceCollection services, string connectionString, BackgroundJobSettings backgroundJobSettings)
        {
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseFilter(new AutomaticRetryAttribute { Attempts = BackgroundJobConst.RETRY_ATTEMPTS, ExceptOn = [typeof(UserFriendlyException)] })
                .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
                {
                    SchemaName = DbSchemas.Hangfire,
                }));
            if (backgroundJobSettings.Enabled)
            {
                services.AddHangfireServer(options =>
                {
                    options.Queues = [BackgroundJobConst.QUEUE_DEFAULT];
                    options.WorkerCount = backgroundJobSettings.WorkerCount;
                });
                services.AddHangfireServer(options =>
                {
                    options.ServerName = $"{Environment.MachineName}:{BackgroundJobConst.QUEUE_EXPORT}";
                    options.Queues = [BackgroundJobConst.QUEUE_EXPORT];
                    options.WorkerCount = backgroundJobSettings.ExportWorkerCount;
                });
            }
            return services;
        }

        public static IApplicationBuilder UseHangfireBackgroundJobDashboard(this IApplicationBuilder app, bool isReadOnly)
        {
            return app.UseHangfireDashboard(BackgroundJobConst.DASHBOARD_PATH, new DashboardOptions
            {
                Authorization = [new AnonymousDashboardAuthorizationFilter()],
                IsReadOnlyFunc = _ => isReadOnly,
            });
        }
    }
}
