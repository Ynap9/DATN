namespace sh.be.shared.Constants.Packaging
{
    public static class PackageSessionConst
    {
        public const string PENDING = "pending";
        public const string RUNNING = "running";
        public const string PASSED = "passed";
        public const string WARNING = "warning";
        public const string FAILED = "failed";
        public const string CANCELLED = "cancelled";

        public static readonly IReadOnlyList<string> CANCELLABLE_STATUSES = [PENDING, RUNNING];

        public const string FILE_ROLE_DATA = "data";
        public const string FILE_ROLE_EXCEL = "excel";
        public const string FILE_ROLE_DIP_AUTH = "dip-auth";

        public const int STATUS_MAX_LENGTH = 20;
        public const int MAX_FILES_PER_UPLOAD = 200;
        public const long MAX_UPLOAD_BYTES = 2L * 1024 * 1024 * 1024;
        public const long MAX_DOCUMENT_BYTES = 100L * 1024 * 1024;
        public const long MAX_MEDIA_BYTES = 1024L * 1024 * 1024;
        public const int UPLOAD_PARALLELISM = 4;
        public const int VERIFY_BATCH_SIZE = 200;
        public const int RETENTION_DAYS = 7;
        public const double HEADER_MATCH_THRESHOLD = 0.8;
        public const int DOC_ORDINAL_LENGTH = 7;

        public const string OBJECT_KEY_PREFIX = "packaging";
        public const string VERIFY_JOB_TYPE = "PackageVerify";
        public const string CLEANUP_RECURRING_JOB_ID = "package-session-cleanup";
        public const string VERIFY_FAILED_MESSAGE = "Lỗi hệ thống khi kiểm phiên đóng gói";

        public const string PDFA_PART_PATTERN = @"pdfaid:part\s*=\s*[""'](\d)[""']|<pdfaid:part>\s*(\d)\s*</pdfaid:part>";
        public const string PDFA_CONFORMANCE_PATTERN = @"pdfaid:conformance\s*=\s*[""']([A-Za-z])[""']|<pdfaid:conformance>\s*([A-Za-z])\s*</pdfaid:conformance>";

        public const string EXCEL_EXTENSION = ".xlsx";
        public const string LEGACY_EXCEL_EXTENSION = ".xls";
        public const string PDF_EXTENSION = ".pdf";

        public const string FILE_CODE = "fileCode";
        public const string ARC_FILE_CODE = "arcFileCode";
        public const string REQUEST_ID = "requestID";
        public const string DOC_ID = "docId";
        public const string DOC_CODE = "docCode";
        public const string ARC_DOC_CODE = "arcDocCode";
        public const string DOC_ORDINAL = "docOrdinal";

        public static readonly IReadOnlyList<string> PACKAGE_CODE_ELEMENTS = [FILE_CODE, ARC_FILE_CODE, REQUEST_ID];
        public static readonly IReadOnlyList<string> SHORT_DATE_ELEMENTS = ["startDate", "endDate", "issuedDate"];
        public static readonly IReadOnlyList<int> SHORT_DATE_PACKAGE_TYPES = [PackageConst.AIP_HOSO, PackageConst.AIP_TAILIEU];

        public static readonly IReadOnlyDictionary<string, string> MIME_TYPES = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [EXCEL_EXTENSION] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [PDF_EXTENSION] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".tif"] = "image/tiff",
            [".tiff"] = "image/tiff",
            [".mp4"] = "video/mpeg",
            [".mpg"] = "video/mpeg",
            [".mpeg"] = "video/mpeg",
            [".avi"] = "video/x-msvideo",
            [".wmv"] = "video/x-ms-wmv",
            [".mov"] = "video/quicktime",
            [".qt"] = "video/quicktime",
            [".mp3"] = "audio/mpeg",
            [".wma"] = "audio/x-ms-wma",
            [".aac"] = "audio/aac",
            [".wav"] = "audio/wav",
        };

        public static readonly IReadOnlyDictionary<int, IReadOnlyList<string>> DATA_EXTENSIONS = new Dictionary<int, IReadOnlyList<string>>
        {
            [PackageSchemaConst.METADATA_TEXT] = [PDF_EXTENSION],
            [PackageSchemaConst.METADATA_PICTURE] = [".jpg", ".jpeg", ".png", ".gif", ".tif", ".tiff", PDF_EXTENSION],
            [PackageSchemaConst.METADATA_MEDIA] = [".mp4", ".mpg", ".mpeg", ".avi", ".wmv", ".mov", ".qt", ".mp3", ".wma", ".aac", ".wav"],
        };
    }
}
