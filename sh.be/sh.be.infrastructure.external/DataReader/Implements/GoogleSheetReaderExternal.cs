using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using sh.be.infrastructure.external.DataReader.Base;
using sh.be.infrastructure.external.DataReader.Models;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Settings;

namespace sh.be.infrastructure.external.DataReader.Implements
{
    public class GoogleSheetReaderExternal : DataReaderExternalBase
    {
        private const string SheetTitleFields = "sheets.properties.title";
        private const int FirstRowIndex = 1;

        private readonly GoogleSheetSettings _googleSheetSettings;
        private readonly ILogger<GoogleSheetReaderExternal> _logger;

        public GoogleSheetReaderExternal(IOptions<GoogleSheetSettings> googleSheetSettings, ILogger<GoogleSheetReaderExternal> logger)
        {
            _googleSheetSettings = googleSheetSettings.Value;
            _logger = logger;
        }

        protected override async Task<List<SheetHeaderRow>> ReadHeaderRowsAsync(string source)
        {
            _logger.LogInformation($"{nameof(ReadHeaderRowsAsync)}, source={source}");
            var spreadsheetId = GoogleSheetUrl.ExtractSpreadsheetId(source);

            using var sheetsService = CreateSheetsService();
            try
            {
                var sheetNames = await GetSheetNamesAsync(sheetsService, spreadsheetId);
                var headerRanges = await GetHeaderRangesAsync(sheetsService, spreadsheetId, sheetNames);

                return sheetNames
                    .Select((sheetName, index) => new SheetHeaderRow
                    {
                        SheetName = sheetName,
                        Cells = ToCells(headerRanges[index]),
                    })
                    .ToList();
            }
            catch (GoogleApiException ex)
            {
                _logger.LogError(ex, $"{nameof(ReadHeaderRowsAsync)}, spreadsheetId={spreadsheetId}");
                throw new UserFriendlyException(ErrorCodes.CoreErrorReadDataSourceFailed);
            }
        }

        protected override async Task<List<string?>> ReadHeaderCellsAsync(string source, string sheetName, int headerRowIndex)
        {
            _logger.LogInformation($"{nameof(ReadHeaderCellsAsync)}, source={source}, sheetName={sheetName}, headerRowIndex={headerRowIndex}");
            var spreadsheetId = GoogleSheetUrl.ExtractSpreadsheetId(source);

            using var sheetsService = CreateSheetsService();
            try
            {
                var sheetNames = await GetSheetNamesAsync(sheetsService, spreadsheetId);
                if (!sheetNames.Contains(sheetName))
                {
                    throw new UserFriendlyException(ErrorCodes.CoreErrorSheetNotFound);
                }

                var headerRange = await sheetsService.Spreadsheets.Values
                    .Get(spreadsheetId, ToHeaderRange(sheetName, headerRowIndex))
                    .ExecuteAsync();
                return ToCells(headerRange);
            }
            catch (GoogleApiException ex)
            {
                _logger.LogError(ex, $"{nameof(ReadHeaderCellsAsync)}, spreadsheetId={spreadsheetId}, sheetName={sheetName}");
                throw new UserFriendlyException(ErrorCodes.CoreErrorReadDataSourceFailed);
            }
        }

        protected override async Task<List<List<string?>>> ReadRowsAsync(string source, string sheetName)
        {
            _logger.LogInformation($"{nameof(ReadRowsAsync)}, source={source}, sheetName={sheetName}");
            var spreadsheetId = GoogleSheetUrl.ExtractSpreadsheetId(source);

            using var sheetsService = CreateSheetsService();
            try
            {
                var sheetNames = await GetSheetNamesAsync(sheetsService, spreadsheetId);
                if (!sheetNames.Contains(sheetName))
                {
                    throw new UserFriendlyException(ErrorCodes.CoreErrorSheetNotFound);
                }

                var sheetRange = await sheetsService.Spreadsheets.Values
                    .Get(spreadsheetId, ToSheetRange(sheetName))
                    .ExecuteAsync();
                return sheetRange.Values?
                    .Select(row => row.Select(cell => cell?.ToString()).ToList())
                    .ToList()
                    ?? new List<List<string?>>();
            }
            catch (GoogleApiException ex)
            {
                _logger.LogError(ex, $"{nameof(ReadRowsAsync)}, spreadsheetId={spreadsheetId}, sheetName={sheetName}");
                throw new UserFriendlyException(ErrorCodes.CoreErrorReadDataSourceFailed);
            }
        }

        private SheetsService CreateSheetsService()
        {
            var credential = CredentialFactory
                .FromFile<ServiceAccountCredential>(_googleSheetSettings.CredentialsPath)
                .ToGoogleCredential()
                .CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);

            return new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = _googleSheetSettings.ApplicationName,
            });
        }

        private static async Task<List<string>> GetSheetNamesAsync(SheetsService sheetsService, string spreadsheetId)
        {
            var spreadsheetRequest = sheetsService.Spreadsheets.Get(spreadsheetId);
            spreadsheetRequest.Fields = SheetTitleFields;
            var spreadsheet = await spreadsheetRequest.ExecuteAsync();
            return spreadsheet.Sheets
                .Select(sheet => sheet.Properties.Title)
                .ToList();
        }

        private static async Task<IList<ValueRange>> GetHeaderRangesAsync(SheetsService sheetsService, string spreadsheetId, List<string> sheetNames)
        {
            var valuesRequest = sheetsService.Spreadsheets.Values.BatchGet(spreadsheetId);
            valuesRequest.Ranges = new Repeatable<string>(sheetNames.Select(sheetName => ToHeaderRange(sheetName, FirstRowIndex)));
            var batchResponse = await valuesRequest.ExecuteAsync();
            return batchResponse.ValueRanges;
        }

        private static string ToHeaderRange(string sheetName, int rowIndex)
        {
            return $"{ToSheetRange(sheetName)}!{rowIndex}:{rowIndex}";
        }

        private static string ToSheetRange(string sheetName)
        {
            return $"'{sheetName.Replace("'", "''")}'";
        }

        private static List<string?> ToCells(ValueRange headerRange)
        {
            return headerRange.Values?
                .FirstOrDefault()?
                .Select(cell => cell?.ToString())
                .ToList()
                ?? new List<string?>();
        }
    }
}
