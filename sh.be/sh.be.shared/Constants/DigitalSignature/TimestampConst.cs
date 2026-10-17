namespace sh.be.shared.Constants.DigitalSignature
{
    public static class TimestampConst
    {
        public const string HTTP_CLIENT_NAME = "Timestamp";
        public const string URL = "http://tsa.ca.gov.vn/";
        public const string CONTENT_TYPE = "application/timestamp-query";
        public const int TIMEOUT_SECONDS = 180;
        public const int MAX_ATTEMPTS = 3;
        public const int RETRY_DELAY_SECONDS = 2;
    }
}
