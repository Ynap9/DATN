using Hangfire.Dashboard;

namespace sh.be.infrastructure.external.BackgroundJob.Base
{
    public class AnonymousDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return true;
        }
    }
}
