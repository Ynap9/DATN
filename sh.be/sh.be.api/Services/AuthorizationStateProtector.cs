using Microsoft.AspNetCore.DataProtection;
using sh.be.applications.Base.Interfaces;
using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Security.Cryptography;

namespace sh.be.api.Services
{
    public class AuthorizationStateProtector : IAuthorizationStateProtector
    {
        private const string Purpose = "GoogleAuthorizationState";

        private readonly ITimeLimitedDataProtector _dataProtector;

        public AuthorizationStateProtector(IDataProtectionProvider dataProtectionProvider)
        {
            _dataProtector = dataProtectionProvider.CreateProtector(Purpose).ToTimeLimitedDataProtector();
        }

        public string CreateState(string? userId)
        {
            return _dataProtector.Protect(userId ?? String.Empty, TimeSpan.FromMinutes(ProjectConfigConst.AUTHORIZATION_STATE_LIFETIME_MINUTES));
        }

        public string? ReadUserId(string? state)
        {
            try
            {
                var userId = _dataProtector.Unprotect(state ?? String.Empty);
                return string.IsNullOrEmpty(userId) ? null : userId;
            }
            catch (CryptographicException)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Phiên xác thực Google không hợp lệ hoặc đã hết hạn");
            }
        }
    }
}
