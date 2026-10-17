using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base.Dtos.Spreadsheet;
using sh.be.applications.Base.Interfaces;
using sh.be.infrastructure.external.Spreadsheet.Base;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.infrastructure.external.Spreadsheet.Implements
{
    public class GoogleSheetAuthorizer : ISpreadsheetAuthorizer
    {
        private readonly ILogger<GoogleSheetAuthorizer> _logger;

        public GoogleSheetAuthorizer(ILogger<GoogleSheetAuthorizer> logger)
        {
            _logger = logger;
        }

        public string BuildAuthorizationUrl(SpreadsheetAccountDto account, string state)
        {
            _logger.LogInformation($"{nameof(BuildAuthorizationUrl)}, clientId={account.ClientId}, redirectUri={account.RedirectUri}");
            using var flow = GoogleOAuthFlowFactory.CreateFlow(account);
            var authorizationRequest = flow.CreateAuthorizationCodeRequest(account.RedirectUri);
            authorizationRequest.State = state;
            return authorizationRequest.Build().AbsoluteUri;
        }

        public async Task<string> ExchangeRefreshTokenAsync(SpreadsheetAccountDto account, string code)
        {
            _logger.LogInformation($"{nameof(ExchangeRefreshTokenAsync)}, clientId={account.ClientId}, redirectUri={account.RedirectUri}");
            using var flow = GoogleOAuthFlowFactory.CreateFlow(account);
            try
            {
                var tokenResponse = await flow.ExchangeCodeForTokenAsync(account.ClientId, code, account.RedirectUri, CancellationToken.None);
                return tokenResponse.RefreshToken
                    ?? throw new UserFriendlyException(ErrorCodes.CoreErrorAuthorizeDataSourceFailed);
            }
            catch (TokenResponseException ex)
            {
                _logger.LogError(ex, $"{nameof(ExchangeRefreshTokenAsync)}, error={ex.Error?.Error}");
                throw new UserFriendlyException(ErrorCodes.CoreErrorAuthorizeDataSourceFailed);
            }
        }
    }
}
