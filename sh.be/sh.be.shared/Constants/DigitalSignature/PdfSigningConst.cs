namespace sh.be.shared.Constants.DigitalSignature
{
    public static class PdfSigningConst
    {
        public const string SUB_FILTER = "ETSI.CAdES.detached";
        public const string DEFAULT_REASON = "Ký số tài liệu";
        public const string DEFAULT_LOCATION = "Việt Nam";
        public const string SIGNATURE_FIELD_PREFIX = "SH_Signature_";
        public const int SIGNATURE_PLACEHOLDER_BYTES = 32768;
        public const int BYTE_RANGE_FIELD_WIDTH = 10;
        public const string PDF_DATE_FORMAT = "yyyyMMddHHmmss";
        public const int VIETNAM_UTC_OFFSET_HOURS = 7;

        public const string SIGNATURE_MARKER_PATTERN = @"/ByteRange\s*\[|/Type\s*/Sig\b";
        public const string BYTE_RANGE_PATTERN = @"/ByteRange\s*\[\s*(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s*\]";
        public const string CONTENTS_PATTERN = @"/Contents\s*<([0-9A-Fa-f]+)>";

        public const string OID_DATA = "1.2.840.113549.1.7.1";
        public const string OID_SIGNED_DATA = "1.2.840.113549.1.7.2";
        public const string OID_CONTENT_TYPE = "1.2.840.113549.1.9.3";
        public const string OID_MESSAGE_DIGEST = "1.2.840.113549.1.9.4";
        public const string OID_SIGNING_TIME = "1.2.840.113549.1.9.5";
        public const string OID_SIGNING_CERTIFICATE_V2 = "1.2.840.113549.1.9.16.2.47";
        public const string OID_SIGNATURE_TIMESTAMP_TOKEN = "1.2.840.113549.1.9.16.2.14";
        public const string OID_SHA256 = "2.16.840.1.101.3.4.2.1";
        public const string OID_RSA_ENCRYPTION = "1.2.840.113549.1.1.1";

        public const string APPEARANCE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ss.fffffff'+07:00'";
        public const double APPEARANCE_PADDING_RATIO = 0.2;
        public const double REFERENCE_PAGE_WIDTH = 595;
        public const double REFERENCE_PAGE_HEIGHT = 842;

        public const double DEFAULT_IMAGE_DPI = 96;
        public const double POINTS_PER_INCH = 72;
        public const double MM_PER_INCH = 25.4;
        public const double STAMP_WIDTH_MM = 36;
        public const double MAX_WET_SIGNATURE_WIDTH_RATIO = 0.25;

        public const double INTENSITY_BRIGHTNESS_FACTOR = 0.5;
        public const double MIN_BRIGHTNESS = 0.5;
        public const double STROKE_RADIUS_RATIO = 0.35 / 158d;
        public const int STROKE_DIRECTIONS = 8;

        public static readonly IReadOnlyList<string> FONT_SEARCH_DIRECTORIES = ["/usr/share/fonts", "/usr/local/share/fonts"];
        public static readonly IReadOnlyList<string> SERIF_REGULAR_FILES = ["times.ttf", "Times_New_Roman.ttf", "LiberationSerif-Regular.ttf", "Tinos-Regular.ttf", "DejaVuSerif.ttf", "NotoSerif-Regular.ttf", "FreeSerif.ttf"];
        public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> FONT_FILES =
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                [TemplateConst.DEFAULT_DIGITAL_SIGNATURE_FONT] = SERIF_REGULAR_FILES,
                ["Arial"] = ["arial.ttf", "LiberationSans-Regular.ttf", "Arimo-Regular.ttf"],
                ["Verdana"] = ["verdana.ttf", "DejaVuSans.ttf"],
                ["Georgia"] = ["georgia.ttf", "Gelasio-Regular.ttf"],
                ["Courier New"] = ["cour.ttf", "Courier_New.ttf", "LiberationMono-Regular.ttf", "Cousine-Regular.ttf"],
            };
    }
}
