namespace sh.be.shared.Constants.Construction
{
    public static class SubmissionConst
    {
        public const int NOT_CHECKED = 0;
        public const int CHECKED = 1;

        public const string NEIGHBOR_NEXT = "next";
        public const string NEIGHBOR_PREVIOUS = "previous";
        public const int MAX_PAGE_SIZE = 200;
        public const int CHECK_NOTE_MAX_LENGTH = 1000;

        public const int IMPORT_LOCK_TIMEOUT_MILLISECONDS = 5000;
        public const int BULK_BATCH_SIZE = 100000;
        public const int BULK_TIMEOUT_SECONDS = 600;
        public const int PARALLEL_ROW_THRESHOLD = 5000;
        public const char KEY_SEPARATOR = '\u001F';
    }
}
