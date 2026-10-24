using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using sh.be.shared.Constants.BackgroundJob;
using sh.be.shared.Constants.Db;
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
                .UseFilter(new AutomaticRetryAttribute { Attempts = BackgroundJobConst.RETRY_ATTEMPTS })
                .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
                {
                    SchemaName = DbSchemas.Hangfire,
                }));
            if (backgroundJobSettings.Enabled)
            {
                services.AddHangfireServer(options => options.WorkerCount = backgroundJobSettings.WorkerCount);
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
