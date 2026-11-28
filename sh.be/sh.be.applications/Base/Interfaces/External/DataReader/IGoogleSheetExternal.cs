using sh.be.applications.Base.Dtos.DataReader;

namespace sh.be.applications.Base.Interfaces.External.DataReader
{
    public interface IGoogleSheetExternal
    {
        string BuildAuthorizationUrl(SpreadsheetAccountDto account, string state);
        Task<string> ExchangeRefreshTokenAsync(SpreadsheetAccountDto account, string code);
        Task<CreatedSpreadsheetDto> CreateSpreadsheetAsync(SpreadsheetAccountDto account, string title, string sheetName);
        Task RenameSpreadsheetAsync(SpreadsheetAccountDto account, string spreadsheetUrl, string title);
        Task<bool> TryRenameSpreadsheetAsync(SpreadsheetAccountDto account, string spreadsheetUrl, string title);
        Task<bool> TryDeleteSpreadsheetAsync(SpreadsheetAccountDto account, string spreadsheetId);
        string ExtractSpreadsheetId(string spreadsheetUrl);
        Task<List<string>> GetSheetNamesAsync(SpreadsheetAccountDto account, string spreadsheetUrl);
        Task WriteTableAsync(SpreadsheetAccountDto account, string spreadsheetUrl, string sheetName, int startRow, List<string> headers, List<List<string>> rows);
        Task ReplaceTableAsync(SpreadsheetAccountDto account, string spreadsheetUrl, string sheetName, List<string> headers, List<List<string>> rows);
    }
}
