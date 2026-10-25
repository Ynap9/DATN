namespace sh.be.shared.Constants.BackgroundJob
{
    public static class BackgroundJobConst
    {
        public const string DASHBOARD_PATH = "/hangfire";
        public const string CONFIG_SECTION = "BackgroundJob";
        public const string SYSTEM_USER = "system";
        public const int RETRY_ATTEMPTS = 2;

        public const string QUEUE_DEFAULT = "default";
        public const string QUEUE_EXPORT = "export";
        public const int SERVER_ALIVE_SECONDS = 90;

        public const int STATUS_WAITING = 0;
        public const int STATUS_RUNNING = 1;
        public const int STATUS_SUCCEEDED = 2;
        public const int STATUS_FAILED = 3;

        public const string PARAMETER_JOB_TYPE = "JobType";
        public const string PARAMETER_USER_ID = "UserId";
        public const string PARAMETER_FILE_NAME = "FileName";
        public const string PARAMETER_OBJECT_KEY = "ObjectKey";
    }
}
