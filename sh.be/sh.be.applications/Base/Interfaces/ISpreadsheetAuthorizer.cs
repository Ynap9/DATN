using sh.be.applications.Base.Dtos.Spreadsheet;

namespace sh.be.applications.Base.Interfaces
{
    public interface ISpreadsheetAuthorizer
    {
        string BuildAuthorizationUrl(SpreadsheetAccountDto account, string state);
        Task<string> ExchangeRefreshTokenAsync(SpreadsheetAccountDto account, string code);
    }
}
