using sh.be.applications.Base.Interfaces.Api;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace sh.be.api.Services
{
    public class HttpRequestContext : IRequestContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpRequestContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId
        {
            get
            {
                var data = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(data))
                {
                    data = _httpContextAccessor.HttpContext?.User.FindFirstValue(Claims.Subject);
                }
                return data;
            }
        }

        public string? Source
        {
            get
            {
                var request = _httpContextAccessor.HttpContext?.Request;
                return request == null ? null : $"{request.Method} {request.Path}";
            }
        }

        public string? ContentType => _httpContextAccessor.HttpContext?.Request.ContentType;
    }
}
