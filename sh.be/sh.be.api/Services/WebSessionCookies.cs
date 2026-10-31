using Microsoft.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using sh.be.shared.Constants.Auth;

namespace sh.be.api.Services
{
    public static class WebSessionCookies
    {
        public static OpenIddictServerBuilder AddWebSessionCookies(this OpenIddictServerBuilder options)
        {
            options.AddEventHandler<OpenIddictServerEvents.ExtractTokenRequestContext>(builder => builder
                .UseInlineHandler(context =>
                {
                    var request = context.Request;
                    if (request is not null
                        && request.ClientId == WebSessionConstants.WEB_CLIENT_ID
                        && request.IsRefreshTokenGrantType()
                        && string.IsNullOrEmpty(request.RefreshToken))
                    {
                        request.RefreshToken = context.Transaction.GetHttpRequest()!.Cookies[WebSessionConstants.REFRESH_TOKEN_COOKIE];
                    }
                    return default;
                })
                .SetOrder(int.MaxValue - 100_000));

            options.AddEventHandler<OpenIddictServerEvents.ApplyTokenResponseContext>(builder => builder
                .UseInlineHandler(context =>
                {
                    var response = context.Response;
                    if (context.Request?.ClientId == WebSessionConstants.WEB_CLIENT_ID && !string.IsNullOrEmpty(response.AccessToken))
                    {
                        var httpRequest = context.Transaction.GetHttpRequest()!;
                        var cookies = httpRequest.HttpContext.Response.Cookies;

                        cookies.Append(
                            WebSessionConstants.ACCESS_TOKEN_COOKIE,
                            response.AccessToken,
                            BuildOptions(httpRequest, "/", TimeSpan.FromSeconds((double)(response.ExpiresIn ?? 3600))));

                        if (!string.IsNullOrEmpty(response.RefreshToken))
                        {
                            cookies.Append(
                                WebSessionConstants.REFRESH_TOKEN_COOKIE,
                                response.RefreshToken,
                                BuildOptions(httpRequest, WebSessionConstants.REFRESH_TOKEN_COOKIE_PATH, TimeSpan.FromDays(WebSessionConstants.REFRESH_TOKEN_COOKIE_DAYS)));
                        }

                        response.AccessToken = null;
                        response.RefreshToken = null;
                        response.IdToken = null;
                    }
                    return default;
                })
                .SetOrder(OpenIddictServerAspNetCoreHandlers.AttachHttpResponseCode<OpenIddictServerEvents.ApplyTokenResponseContext>.Descriptor.Order - 1_000));

            return options;
        }

        public static void Clear(HttpContext httpContext)
        {
            var cookies = httpContext.Response.Cookies;
            cookies.Delete(WebSessionConstants.ACCESS_TOKEN_COOKIE, BuildOptions(httpContext.Request, "/", null));
            cookies.Delete(WebSessionConstants.REFRESH_TOKEN_COOKIE, BuildOptions(httpContext.Request, WebSessionConstants.REFRESH_TOKEN_COOKIE_PATH, null));
        }

        private static CookieOptions BuildOptions(HttpRequest request, string path, TimeSpan? maxAge)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Path = path,
                MaxAge = maxAge,
            };
        }
    }
}
