using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.RegularExpressions;

namespace sh.be.infrastructure.external.DataReader.Base
{
    public static class GoogleSheetUrl
    {
        private const string UrlFormat = "https://docs.google.com/spreadsheets/d/{0}/edit";
        private static readonly Regex SpreadsheetIdRegex = new(@"/spreadsheets/d/([a-zA-Z0-9-_]+)", RegexOptions.Compiled);

        public static string ToUrl(string spreadsheetId)
        {
            return string.Format(UrlFormat, spreadsheetId);
        }

        public static string ExtractSpreadsheetId(string url)
        {
            var match = SpreadsheetIdRegex.Match(url ?? String.Empty);
            if (!match.Success)
            {
                throw new UserFriendlyException(ErrorCodes.CoreErrorInvalidDataSource);
            }
            return match.Groups[1].Value;
        }
    }
}
