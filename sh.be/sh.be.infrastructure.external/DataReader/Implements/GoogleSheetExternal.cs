using Google;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Base.Interfaces.External.DataReader;
using sh.be.infrastructure.external.DataReader.Base;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Settings;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace sh.be.infrastructure.external.DataReader.Implements
{
    public class GoogleSheetExternal : IGoogleSheetExternal
    {
        private const string SpreadsheetMimeType = "application/vnd.google-apps.spreadsheet";
        private const string SheetTitleField = "title";
        private const int FirstSheetId = 0;

        private readonly GoogleSheetSettings _googleSheetSettings;
        private readonly ILogger<GoogleSheetExternal> _logger;

        public GoogleSheetExternal(IOptions<GoogleSheetSettings> googleSheetSettings, ILogger<GoogleSheetExternal> logger)
        {
            _googleSheetSettings = googleSheetSettings.Value;
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

        public async Task<CreatedSpreadsheetDto> CreateSpreadsheetAsync(SpreadsheetAccountDto account, string title, string sheetName)
        {
            _logger.LogInformation($"{nameof(CreateSpreadsheetAsync)}, title={title}, sheetName={sheetName}, folderId={account.FolderId}");
            using var flow = GoogleOAuthFlowFactory.CreateFlow(account);
            var initializer = CreateServiceInitializer(flow, account);
            using var driveService = new DriveService(initializer);
            using var sheetsService = new SheetsService(initializer);

            try
            {
                var spreadsheetId = await CreateSpreadsheetFileAsync(driveService, title, account.FolderId);
                try
                {
                    await RenameFirstSheetAsync(sheetsService, spreadsheetId, sheetName);
                }
                catch (GoogleApiException)
                {
                    await TryDeleteSpreadsheetAsync(account, spreadsheetId);
                    throw;
                }

                return new CreatedSpreadsheetDto
                {
                    SpreadsheetId = spreadsheetId,
                    Url = GoogleSheetUrl.ToUrl(spreadsheetId),
                };
            }
            catch (TokenResponseException ex)
            {
                _logger.LogError(ex, $"{nameof(CreateSpreadsheetAsync)}, error={ex.Error?.Error}");
                throw new UserFriendlyException(ErrorCodes.CoreErrorAuthorizeDataSourceFailed);
            }
            catch (GoogleApiException ex)
            {
                _logger.LogError(ex, $"{nameof(CreateSpreadsheetAsync)}, title={title}, folderId={account.FolderId}");
                throw new UserFriendlyException(ErrorCodes.CoreErrorCreateDataSourceFailed);
            }
        }

        public async Task RenameSpreadsheetAsync(SpreadsheetAccountDto account, string spreadsheetUrl, string title)
        {
            _logger.LogInformation($"{nameof(RenameSpreadsheetAsync)}, spreadsheetUrl={spreadsheetUrl}, title={title}");
            var spreadsheetId = GoogleSheetUrl.ExtractSpreadsheetId(spreadsheetUrl);
            using var flow = GoogleOAuthFlowFactory.CreateFlow(account);
            using var driveService = new DriveService(CreateServiceInitializer(flow, account));
            try
            {
                var updateRequest = driveService.Files.Update(new DriveFile { Name = title }, spreadsheetId);
                updateRequest.SupportsAllDrives = true;
                await updateRequest.ExecuteAsync();
            }
            catch (TokenResponseException ex)
            {
                _logger.LogError(ex, $"{nameof(RenameSpreadsheetAsync)}, error={ex.Error?.Error}");
                throw new UserFriendlyException(ErrorCodes.CoreErrorAuthorizeDataSourceFailed);
            }
            catch (GoogleApiException ex)
            {
                _logger.LogError(ex, $"{nameof(RenameSpreadsheetAsync)}, spreadsheetId={spreadsheetId}");
                throw new UserFriendlyException(ErrorCodes.CoreErrorUpdateDataSourceFailed);
            }
        }

        public async Task<bool> TryRenameSpreadsheetAsync(SpreadsheetAccountDto account, string spreadsheetUrl, string title)
        {
            try
            {
                await RenameSpreadsheetAsync(account, spreadsheetUrl, title);
                return true;
            }
            catch (UserFriendlyException)
            {
                return false;
            }
        }

        public async Task<bool> TryDeleteSpreadsheetAsync(SpreadsheetAccountDto account, string spreadsheetId)
        {
            _logger.LogInformation($"{nameof(TryDeleteSpreadsheetAsync)}, spreadsheetId={spreadsheetId}");
            using var flow = GoogleOAuthFlowFactory.CreateFlow(account);
            using var driveService = new DriveService(CreateServiceInitializer(flow, account));
            try
            {
                var deleteRequest = driveService.Files.Delete(spreadsheetId);
                deleteRequest.SupportsAllDrives = true;
                await deleteRequest.ExecuteAsync();
                return true;
            }
            catch (Exception ex) when (ex is GoogleApiException or TokenResponseException)
            {
                _logger.LogError(ex, $"{nameof(TryDeleteSpreadsheetAsync)}, spreadsheetId={spreadsheetId}");
                return false;
            }
        }

        private static async Task<string> CreateSpreadsheetFileAsync(DriveService driveService, string title, string folderId)
        {
            var createRequest = driveService.Files.Create(new DriveFile
            {
                Name = title,
                MimeType = SpreadsheetMimeType,
                Parents = [folderId],
            });
            createRequest.Fields = "id";
            createRequest.SupportsAllDrives = true;
            var spreadsheetFile = await createRequest.ExecuteAsync();
            return spreadsheetFile.Id;
        }

        private static Task RenameFirstSheetAsync(SheetsService sheetsService, string spreadsheetId, string sheetName)
        {
            var renameRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests =
                [
                    new Request
                    {
                        UpdateSheetProperties = new UpdateSheetPropertiesRequest
                        {
                            Properties = new SheetProperties { SheetId = FirstSheetId, Title = sheetName },
                            Fields = SheetTitleField,
                        },
                    },
                ],
            };
            return sheetsService.Spreadsheets.BatchUpdate(renameRequest, spreadsheetId).ExecuteAsync();
        }

        private BaseClientService.Initializer CreateServiceInitializer(GoogleAuthorizationCodeFlow flow, SpreadsheetAccountDto account)
        {
            return new BaseClientService.Initializer
            {
                HttpClientInitializer = GoogleOAuthFlowFactory.CreateUserCredential(flow, account),
                ApplicationName = _googleSheetSettings.ApplicationName,
            };
        }
    }
}
