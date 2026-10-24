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
    }
}
