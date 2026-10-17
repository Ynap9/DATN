namespace sh.be.applications.Base.Dtos.Spreadsheet
{
    public class SpreadsheetAccountDto
    {
        public string ClientId { get; set; } = String.Empty;
        public string ClientSecret { get; set; } = String.Empty;
        public string RedirectUri { get; set; } = String.Empty;
        public string RefreshToken { get; set; } = String.Empty;
        public string FolderId { get; set; } = String.Empty;
    }
}
