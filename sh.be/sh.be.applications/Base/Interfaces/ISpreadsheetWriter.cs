using sh.be.applications.Base.Dtos.Spreadsheet;

namespace sh.be.applications.Base.Interfaces
{
    public interface ISpreadsheetWriter
    {
        Task<CreatedSpreadsheetDto> CreateSpreadsheetAsync(SpreadsheetAccountDto account, string title, string sheetName);
        Task RenameSpreadsheetAsync(SpreadsheetAccountDto account, string spreadsheetUrl, string title);
        Task<bool> TryRenameSpreadsheetAsync(SpreadsheetAccountDto account, string spreadsheetUrl, string title);
        Task<bool> TryDeleteSpreadsheetAsync(SpreadsheetAccountDto account, string spreadsheetId);
    }
}
