using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using sh.be.shared.Constants.SignalR;

namespace sh.be.infrastructure.external.SignalR.Base
{
    public static class SignalRExtensions
    {
        public static IServiceCollection AddSignalRNotification(this IServiceCollection services)
        {
            services.AddSignalR();
            services.AddSingleton<SignalRConnectionTracker>();
            return services;
        }

        public static HubEndpointConventionBuilder MapSignalRNotificationHub(this IEndpointRouteBuilder endpoints, string authenticationScheme)
        {
            return endpoints.MapHub<NotificationHub>(SignalRConst.HUB_PATH)
                .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = authenticationScheme })
                .RequireCors(SignalRConst.CORS_POLICY);
        }
    }
}
