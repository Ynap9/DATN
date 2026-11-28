namespace sh.be.shared.Constants.Packaging
{
    public static class PackageBuildConst
    {
        public const string CONFIG_SECTION = "Packaging";

        public const string DONE = "done";
        public static readonly IReadOnlyList<string> BUILDABLE_STATUSES = [PackageSessionConst.PASSED, PackageSessionConst.WARNING, PackageSessionConst.FAILED];

        public const int FONDS_CODE_MAX_LENGTH = 50;
        public const string FONDS_CODE_PATTERN = "^[A-Za-z0-9_-]+$";
        public const int ARCHIVE_MAX_LENGTH = 100;
        public const int BUILD_ERROR_MAX_LENGTH = 1000;

        public const string BUILD_JOB_TYPE = "PackageBuild";
        public const string BUILD_FAILED_MESSAGE = "Lỗi hệ thống khi dựng gói";
        public const string ALL_PACKAGES_FAILED_MESSAGE = "Không dựng được gói nào, xem lỗi từng gói";
        public const string BUILD_ERROR_PREFIX = "Dựng gói lỗi: ";
        public const string PACKAGES_FOLDER = "packages";
        public const string ZIP_EXTENSION = ".zip";
        public const string ZIP_MIME_TYPE = "application/zip";
        public const int PACKAGE_LINK_HOURS = 1;
        public static readonly TimeSpan VIETNAM_OFFSET = TimeSpan.FromHours(7);

        public const string TITLE = "title";
        public const string CSIP_PROFILE = "https://earkcsip.dilcis.eu/profile/E-ARK-CSIP.xml";

        public static readonly IReadOnlyList<int> AIP_PACKAGE_TYPES = [PackageConst.AIP_HOSO, PackageConst.AIP_TAILIEU];
        public static readonly IReadOnlyList<int> PRESERVATION_PACKAGE_TYPES = [PackageConst.AIP_HOSO, PackageConst.AIP_TAILIEU, PackageConst.DIP];

        public static readonly IReadOnlyDictionary<int, string> METS_TYPES = new Dictionary<int, string>
        {
            [PackageConst.AIP_HOSO] = "Mixed",
            [PackageConst.AIP_TAILIEU] = "Collection",
            [PackageConst.SIP_HOSO] = "Mixed",
            [PackageConst.SIP_TAILIEU] = "Collection",
            [PackageConst.DIP] = "MIXED",
        };

        public static readonly IReadOnlyDictionary<int, string> METS_PROFILES = new Dictionary<int, string>
        {
            [PackageConst.AIP_HOSO] = "http://www.ra.ee/METS/v01/IP.xml",
            [PackageConst.AIP_TAILIEU] = "http://www.ra.ee/METS/v01/IP.xml",
            [PackageConst.SIP_HOSO] = CSIP_PROFILE,
            [PackageConst.SIP_TAILIEU] = CSIP_PROFILE,
            [PackageConst.DIP] = "https://earkdip.dilcis.eu/profile/E-ARK-DIP.xml",
        };

        public static readonly IReadOnlyDictionary<int, string> OAIS_PACKAGE_TYPES = new Dictionary<int, string>
        {
            [PackageConst.AIP_HOSO] = "AIP",
            [PackageConst.AIP_TAILIEU] = "AIP",
            [PackageConst.SIP_HOSO] = "SIP",
            [PackageConst.SIP_TAILIEU] = "SIP",
            [PackageConst.DIP] = "DIP",
        };

        public static readonly IReadOnlyDictionary<int, string> REPRESENTATION_DIV_TYPES = new Dictionary<int, string>
        {
            [PackageConst.AIP_HOSO] = "NORMALIZED",
            [PackageConst.AIP_TAILIEU] = "NORMALIZED",
            [PackageConst.SIP_HOSO] = "ORIGINAL",
            [PackageConst.SIP_TAILIEU] = "ORIGINAL",
            [PackageConst.DIP] = "ORIGINAL",
        };

        public static readonly IReadOnlyDictionary<int, string> PREMIS_EVENT_TYPES = new Dictionary<int, string>
        {
            [PackageConst.AIP_HOSO] = "transfer",
            [PackageConst.AIP_TAILIEU] = "transfer",
            [PackageConst.DIP] = "dissemination",
        };

        public static readonly IReadOnlyDictionary<int, string> PREMIS_OBJECT_IDENTIFIER_TYPES = new Dictionary<int, string>
        {
            [PackageSchemaConst.METADATA_TEXT] = "Doc",
            [PackageSchemaConst.METADATA_PICTURE] = "Pic",
            [PackageSchemaConst.METADATA_MEDIA] = "Media",
        };

        public static readonly IReadOnlyDictionary<int, IReadOnlyList<string>> SIP_HOSO_ELEMENT_ORDERS = new Dictionary<int, IReadOnlyList<string>>
        {
            [PackageSchemaConst.METADATA_PACKAGE] =
            [
                "confidenceLevel", "totalDoc", "endDate", "inforSign", "format", "fileCode", "description", "language", "paperFileCode",
                "riskRecoveryStatus", "title", "numberOfPage", "mode", "keyword", "numberOfPaper", "maintenance", "startDate", "riskRecovery",
            ],
            [PackageSchemaConst.METADATA_TEXT] =
            [
                "issuedDate", "confidenceLevel", "process", "docId", "subject", "inforSign", "typeName", "format", "docCode", "description",
                "language", "autograph", "riskRecoveryStatus", "codeNumber", "numberOfPage", "mode", "organName", "codeNotation", "keyword",
                "maintenance", "riskRecovery",
            ],
        };
    }
}
