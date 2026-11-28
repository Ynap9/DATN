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
using System.Net;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace sh.be.infrastructure.external.DataReader.Implements
{
    public class GoogleSheetExternal : IGoogleSheetExternal
    {
        private const string SpreadsheetMimeType = "application/vnd.google-apps.spreadsheet";
        private const string SheetTitleField = "title";
        private const int FirstSheetId = 0;
        private const string SheetNamesFields = "sheets.properties.title";
        private const string SheetGridFields = "sheets.properties(sheetId,title,gridProperties(rowCount,columnCount))";
        private const string RowDimension = "ROWS";
        private const string ColumnDimension = "COLUMNS";
        private const int WriteBatchRowCount = 5000;
        private const int ServerErrorStatusCode = 500;

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

        public string ExtractSpreadsheetId(string spreadsheetUrl)
        {
            return GoogleSheetUrl.ExtractSpreadsheetId(spreadsheetUrl);
        }

        public async Task<List<string>> GetSheetNamesAsync(SpreadsheetAccountDto account, string spreadsheetUrl)
        {
            _logger.LogInformation($"{nameof(GetSheetNamesAsync)}, spreadsheetUrl={spreadsheetUrl}");
            var spreadsheetId = GoogleSheetUrl.ExtractSpreadsheetId(spreadsheetUrl);
            using var flow = GoogleOAuthFlowFactory.CreateFlow(account);
            using var sheetsService = new SheetsService(CreateServiceInitializer(flow, account));
            try
            {
                var sheets = await GetSheetsAsync(sheetsService, spreadsheetId, SheetNamesFields);
                return sheets.Select(sheet => sheet.Properties.Title).ToList();
            }
            catch (TokenResponseException ex)
            {
                _logger.LogError(ex, $"{nameof(GetSheetNamesAsync)}, error={ex.Error?.Error}");
                throw new UserFriendlyException(ErrorCodes.CoreErrorAuthorizeDataSourceFailed);
            }
            catch (GoogleApiException ex)
            {
                _logger.LogError(ex, $"{nameof(GetSheetNamesAsync)}, spreadsheetId={spreadsheetId}");
                throw new UserFriendlyException(ErrorCodes.CoreErrorReadDataSourceFailed);
            }
        }

        // Ghi tiêu đề ở startRow, dữ liệu từ dòng kế tiếp (ô thừa bên phải ghi đè rỗng) rồi mới xoá các dòng dư phía dưới, nên lỗi giữa chừng không để tab trống;
        // các dòng phía trên startRow giữ nguyên. Lỗi tạm thời của Google (429 / 5xx) ném nguyên để Hangfire thử lại
        public async Task WriteTableAsync(SpreadsheetAccountDto account, string spreadsheetUrl, string sheetName, int startRow, List<string> headers,
            List<List<string>> rows)
        {
            _logger.LogInformation($"{nameof(WriteTableAsync)}, spreadsheetUrl={spreadsheetUrl}, sheetName={sheetName}, startRow={startRow}, rowCount={rows.Count}");
            var spreadsheetId = GoogleSheetUrl.ExtractSpreadsheetId(spreadsheetUrl);
            using var flow = GoogleOAuthFlowFactory.CreateFlow(account);
            using var sheetsService = new SheetsService(CreateServiceInitializer(flow, account));
            try
            {
                var sheets = await GetSheetsAsync(sheetsService, spreadsheetId, SheetGridFields);
                var sheetProperties = sheets.Select(sheet => sheet.Properties).FirstOrDefault(properties => properties.Title == sheetName)
                    ?? throw new UserFriendlyException(ErrorCodes.CoreErrorSheetNotFound);
                var (rowCount, columnCount) = await EnsureGridSizeAsync(sheetsService, spreadsheetId, sheetProperties, startRow + rows.Count, headers.Count);

                var values = rows.Prepend(headers)
                    .Select(row => (IList<object>)row.Concat(Enumerable.Repeat(string.Empty, columnCount - row.Count)).Cast<object>().ToList())
                    .ToList();
                var quotedSheetName = QuoteSheetName(sheetName);
                await WriteValuesAsync(sheetsService, spreadsheetId, quotedSheetName, startRow, values);

                var firstLeftoverRow = startRow + values.Count;
                if (firstLeftoverRow <= rowCount)
                {
                    await sheetsService.Spreadsheets.Values.Clear(new ClearValuesRequest(), spreadsheetId, $"{quotedSheetName}!{firstLeftoverRow}:{rowCount}").ExecuteAsync();
                }
            }
            catch (TokenResponseException ex)
            {
                _logger.LogError(ex, $"{nameof(WriteTableAsync)}, error={ex.Error?.Error}");
                throw new UserFriendlyException(ErrorCodes.CoreErrorAuthorizeDataSourceFailed);
            }
            catch (GoogleApiException ex) when (!IsTransient(ex))
            {
                _logger.LogError(ex, $"{nameof(WriteTableAsync)}, spreadsheetId={spreadsheetId}, sheetName={sheetName}");
                throw new UserFriendlyException(ErrorCodes.CoreErrorUpdateDataSourceFailed);
            }
        }

        // Ghi bảng vào tab theo kiểu thay thế: chưa có tab thì tạo, có rồi thì xoá hết nội dung (kể cả ô nhập tay) rồi ghi lại từ A1.
        // Lỗi tạm thời của Google (429 / 5xx) ném nguyên để Hangfire thử lại
        public async Task ReplaceTableAsync(SpreadsheetAccountDto account, string spreadsheetUrl, string sheetName, List<string> headers, List<List<string>> rows)
        {
            _logger.LogInformation($"{nameof(ReplaceTableAsync)}, spreadsheetUrl={spreadsheetUrl}, sheetName={sheetName}, rowCount={rows.Count}");
            var spreadsheetId = GoogleSheetUrl.ExtractSpreadsheetId(spreadsheetUrl);
            using var flow = GoogleOAuthFlowFactory.CreateFlow(account);
            using var sheetsService = new SheetsService(CreateServiceInitializer(flow, account));
            try
            {
                var sheets = await GetSheetsAsync(sheetsService, spreadsheetId, SheetGridFields);
                var sheetProperties = sheets.Select(sheet => sheet.Properties).FirstOrDefault(properties => string.Equals(properties.Title, sheetName, StringComparison.OrdinalIgnoreCase));
                if (sheetProperties == null)
                {
                    var addSheetRequest = new BatchUpdateSpreadsheetRequest
                    {
                        Requests = [new Request { AddSheet = new AddSheetRequest { Properties = new SheetProperties { Title = sheetName } } }],
                    };
                    sheetProperties = (await sheetsService.Spreadsheets.BatchUpdate(addSheetRequest, spreadsheetId).ExecuteAsync()).Replies[0].AddSheet.Properties;
                }

                var quotedSheetName = QuoteSheetName(sheetName);
                await sheetsService.Spreadsheets.Values.Clear(new ClearValuesRequest(), spreadsheetId, quotedSheetName).ExecuteAsync();
                await EnsureGridSizeAsync(sheetsService, spreadsheetId, sheetProperties, rows.Count + 1, headers.Count);
                await WriteValuesAsync(sheetsService, spreadsheetId, quotedSheetName, 1, rows.Prepend(headers).Select(row => (IList<object>)row.Cast<object>().ToList()).ToList());
            }
            catch (TokenResponseException ex)
            {
                _logger.LogError(ex, $"{nameof(ReplaceTableAsync)}, error={ex.Error?.Error}");
                throw new UserFriendlyException(ErrorCodes.CoreErrorAuthorizeDataSourceFailed);
            }
            catch (GoogleApiException ex) when (!IsTransient(ex))
            {
                _logger.LogError(ex, $"{nameof(ReplaceTableAsync)}, spreadsheetId={spreadsheetId}, sheetName={sheetName}");
                throw new UserFriendlyException(ErrorCodes.CoreErrorUpdateDataSourceFailed);
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

        private static async Task<IList<Sheet>> GetSheetsAsync(SheetsService sheetsService, string spreadsheetId, string fields)
        {
            var getRequest = sheetsService.Spreadsheets.Get(spreadsheetId);
            getRequest.Fields = fields;
            var spreadsheet = await getRequest.ExecuteAsync();
            return spreadsheet.Sheets ?? [];
        }

        // Lưới sheet thiếu dòng / cột thì thêm cho đủ (ghi ra ngoài lưới Google báo lỗi); trả số dòng và số cột của lưới sau khi thêm
        private static async Task<(int RowCount, int ColumnCount)> EnsureGridSizeAsync(SheetsService sheetsService, string spreadsheetId, SheetProperties sheetProperties,
            int requiredRowCount, int requiredColumnCount)
        {
            var rowCount = sheetProperties.GridProperties?.RowCount ?? 0;
            var columnCount = sheetProperties.GridProperties?.ColumnCount ?? 0;
            var requests = new List<Request>();
            if (requiredRowCount > rowCount)
            {
                requests.Add(CreateAppendDimensionRequest(sheetProperties.SheetId, RowDimension, requiredRowCount - rowCount));
            }
            if (requiredColumnCount > columnCount)
            {
                requests.Add(CreateAppendDimensionRequest(sheetProperties.SheetId, ColumnDimension, requiredColumnCount - columnCount));
            }

            if (requests.Count > 0)
            {
                await sheetsService.Spreadsheets.BatchUpdate(new BatchUpdateSpreadsheetRequest { Requests = requests }, spreadsheetId).ExecuteAsync();
            }
            return (Math.Max(rowCount, requiredRowCount), Math.Max(columnCount, requiredColumnCount));
        }

        private static Request CreateAppendDimensionRequest(int? sheetId, string dimension, int length)
        {
            return new Request
            {
                AppendDimension = new AppendDimensionRequest { SheetId = sheetId, Dimension = dimension, Length = length },
            };
        }

        // Quá giới hạn gọi (429) hoặc Google lỗi máy chủ (5xx) thì thử lại được, khác lỗi quyền / dữ liệu
        private static bool IsTransient(GoogleApiException ex)
        {
            return ex.HttpStatusCode == HttpStatusCode.TooManyRequests || (int)ex.HttpStatusCode >= ServerErrorStatusCode;
        }

        // Ghi RAW để giữ nguyên "001", ngày dạng chữ; chia lô để mỗi lần gọi không quá lớn
        private static async Task WriteValuesAsync(SheetsService sheetsService, string spreadsheetId, string quotedSheetName, int startRow,
            List<IList<object>> values)
        {
            for (var offset = 0; offset < values.Count; offset += WriteBatchRowCount)
            {
                var valueRange = new ValueRange { Values = values.Skip(offset).Take(WriteBatchRowCount).ToList() };
                var updateRequest = sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, $"{quotedSheetName}!A{startRow + offset}");
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                await updateRequest.ExecuteAsync();
            }
        }

        // Tên sheet trong A1 notation đặt trong nháy đơn, nháy đơn trong tên nhân đôi
        private static string QuoteSheetName(string sheetName)
        {
            return $"'{sheetName.Replace("'", "''")}'";
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
