namespace sh.be.shared.Constants.Auth
{
    public static class WebSessionConstants
    {
        public const string WEB_CLIENT_ID = "client-web2";
        public const string ACCESS_TOKEN_COOKIE = "sh_access_token";
        public const string REFRESH_TOKEN_COOKIE = "sh_refresh_token";
        public const string REFRESH_TOKEN_COOKIE_PATH = "/connect";
        public const int REFRESH_TOKEN_COOKIE_DAYS = 14;
    }
}
