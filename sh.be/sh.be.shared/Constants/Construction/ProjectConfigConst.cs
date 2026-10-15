namespace sh.be.shared.Constants.Construction
{
    public static class ProjectConfigConst
    {
        public const string GOOGLE_CLIENT_ID = "GoogleClientId";
        public const string GOOGLE_CLIENT_SECRET = "GoogleClientSecret";
        public const string GOOGLE_REDIRECT_URI = "GoogleRedirectUri";
        public const string GOOGLE_FOLDER_ID = "GoogleFolderId";
        public const string GOOGLE_REFRESH_TOKEN = "GoogleRefreshToken";

        public static readonly string[] SECRET_NAMES = [GOOGLE_CLIENT_SECRET, GOOGLE_REFRESH_TOKEN];
        public const string MASKED_VALUE = "******";

        public const int AUTHORIZATION_STATE_LIFETIME_MINUTES = 10;
    }
}
