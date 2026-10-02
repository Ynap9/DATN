namespace sh.be.shared.Settings
{
    public class EmailSettings
    {
        public SmtpSettings Smtp { get; set; } = new();
        public string DefaultFromAddress { get; set; } = String.Empty;
        public string DefaultFromDisplayName { get; set; } = String.Empty;
    }

    public class SmtpSettings
    {
        public string Host { get; set; } = String.Empty;
        public int Port { get; set; }
        public string UserName { get; set; } = String.Empty;
        public string Password { get; set; } = String.Empty;
        public string? Domain { get; set; }
        public bool EnableSsl { get; set; }
        public bool UseDefaultCredentials { get; set; }
    }
}
