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

        public const int MAX_PAGE_SIZE = 200;
        public const int CHECK_NOTE_MAX_LENGTH = 1000;

        public const int IMPORT_LOCK_TIMEOUT_MILLISECONDS = 5000;
        public const int BULK_BATCH_SIZE = 100000;
        public const int BULK_TIMEOUT_SECONDS = 600;
        public const int PARALLEL_ROW_THRESHOLD = 5000;
        public const char KEY_SEPARATOR = '\u001F';
    }
}
