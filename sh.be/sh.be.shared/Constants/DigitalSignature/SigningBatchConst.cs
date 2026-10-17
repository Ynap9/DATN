namespace sh.be.shared.Constants.DigitalSignature
{
    public static class SigningBatchConst
    {
        public const int NEW = 0;
        public const int SIGNING = 1;
        public const int DONE = 2;
        public const int CANCELLED = 3;
        public const int FAILED = 4;
        public const int PAUSED = 5;

        public static readonly IReadOnlyList<int> STATUSES = [NEW, SIGNING, DONE, CANCELLED, FAILED, PAUSED];
        public static readonly IReadOnlyList<int> RUNNING_STATUSES = [SIGNING, PAUSED];
        public static readonly IReadOnlyList<int> SESSION_OPENABLE_STATUSES = [NEW, PAUSED, FAILED];
        public static readonly IReadOnlyList<int> FINISHED_STATUSES = [DONE, CANCELLED];

        public const int MAX_FILES_PER_UPLOAD = 200;
        public const long MAX_UPLOAD_BYTES = 512L * 1024 * 1024;
        public const int UPLOAD_PARALLELISM = 4;

        public const string PDF_EXTENSION = ".pdf";
        public const string PDF_CONTENT_TYPE = "application/pdf";
        public const string OBJECT_KEY_PREFIX = "digital-signature/signing-batch";
        public const string SOURCE_FOLDER = "source";
        public const string SIGNED_FOLDER = "signed";
        public const int SIGNED_URL_MINUTES = 15;

        public const int MAX_RUNNING_BATCHES = 3;
        public const int PREPARE_WORKERS = 4;
        public const int TSA_CONNECTIONS = 4;
        public const int FINISH_SLOTS = 4;
        public const int TSA_FAILURES_TO_PAUSE = 5;
        public const int REQUEST_BATCH_SIZE = 8;
        public const int PREFETCH_FILES = 16;
        public const int LONG_POLL_SECONDS = 25;
        public const int SIGNATURE_TIMEOUT_SECONDS = 120;

        public const string FAILED_FILES_REASON = "Còn file ký lỗi. Mở phiên rồi ký tiếp để ký lại các file lỗi";
        public const string PAUSED_BY_USER_REASON = "Người dùng tạm dừng";
        public const string PAUSED_BY_TSA_REASON = "TSA lỗi liên tiếp nên lô tạm dừng. Thử ký tiếp sau ít phút";
        public const string PAUSED_BY_SESSION_REASON = "Phiên ký đã đóng (plugin tắt, rút token hoặc đóng tab)";
        public const string PAUSED_BY_RESTART_REASON = "Máy chủ khởi động lại khi lô đang ký";
        public const string FAILED_BY_SYSTEM_REASON = "Lỗi hệ thống khi ký lô";
        public const int MAX_ERROR_MESSAGE_LENGTH = 1000;
    }
}
