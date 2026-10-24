using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Sheets.v4;
using sh.be.applications.Base.Dtos.DataReader;

namespace sh.be.infrastructure.external.DataReader.Base
{
    public static class GoogleOAuthFlowFactory
    {
        private const string ConsentPrompt = "consent";
        private static readonly string[] Scopes = [DriveService.Scope.Drive, SheetsService.Scope.Spreadsheets];

        public static GoogleAuthorizationCodeFlow CreateFlow(SpreadsheetAccountDto account)
        {
            return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets { ClientId = account.ClientId, ClientSecret = account.ClientSecret },
                Scopes = Scopes,
                Prompt = ConsentPrompt,
            });
        }

        public static UserCredential CreateUserCredential(GoogleAuthorizationCodeFlow flow, SpreadsheetAccountDto account)
        {
            return new UserCredential(flow, account.ClientId, new TokenResponse { RefreshToken = account.RefreshToken });
        }
    }
}
