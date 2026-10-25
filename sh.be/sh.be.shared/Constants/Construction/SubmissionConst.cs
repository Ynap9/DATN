namespace sh.be.shared.Constants.Construction
{
    public static class SubmissionConst
    {
        public const int NOT_CHECKED = 0;
        public const int CHECKED = 1;
        public const int RETURNED = 2;
        public static readonly IReadOnlyList<int> CHECK_STATUSES = [NOT_CHECKED, CHECKED, RETURNED];

        public const int QA_NOT_REVIEWED = 0;
        public const int QA_PASSED = 1;
        public const int QA_FAILED = 2;
        public static readonly IReadOnlyList<int> QA_STATUSES = [QA_NOT_REVIEWED, QA_PASSED, QA_FAILED];
        public static readonly IReadOnlyList<int> QA_RESULTS = [QA_PASSED, QA_FAILED];
        public const int QA_REASON_MAX_LENGTH = 1000;

        public const int QA_STATS_DEFAULT_DAYS = 30;
        public const int QA_STATS_MAX_DAYS = 366;
        public const string QA_STATS_ORDER_REVIEW = "review";
        public const string QA_STATS_ORDER_ACCURACY = "accuracy";
        public const string UNKNOWN_ENTRY_USER_NAME = "Không xác định";
        public const string DELETED_FIELD_NAME_SUFFIX = " (đã xoá)";
        public const int TIMELINE_MAX_EVENTS = 500;
        public const string TIMELINE_EVENT_CHECK = "CHECK";
        public const string TIMELINE_EVENT_QA = "QA";

        public const int MAX_PAGE_SIZE = 200;
        public const int CHECK_NOTE_MAX_LENGTH = 1000;
        public const int FIELD_VALUE_MAX_ITEMS = 200;
        public const int DELETE_MAX_ITEMS = 500;
        public const int EXPORT_MAX_ROWS = 1048575;
        public const string EXPORT_JOB_TYPE = "submission-export";
        public const string EXPORT_OBJECT_KEY_PREFIX = "construction/submission-export";
        public const int EXPORT_DOWNLOAD_URL_MINUTES = 15;
        public const int EXPORT_FILE_RETENTION_DAYS = 1;
        public const int EXPORT_CLEANUP_LOOKBACK_DAYS = 7;
        public const string EXPORT_CLEANUP_RECURRING_JOB_ID = "submission-export-cleanup";
        public const string EXPORT_FAILED_MESSAGE = "Xuất Excel thất bại, hãy thử lại";

        public const int IMPORT_LOCK_TIMEOUT_MILLISECONDS = 5000;
        public const int BULK_BATCH_SIZE = 100000;
        public const int BULK_TIMEOUT_SECONDS = 600;
        public const int PARALLEL_ROW_THRESHOLD = 5000;
        public const char KEY_SEPARATOR = '\u001F';
    }
}
