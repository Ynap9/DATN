namespace sh.be.shared.Constants.DigitalSignature
{
    public static class TemplateConst
    {
        public const int DEFAULT_INTENSITY = 140;
        public const int MIN_INTENSITY = 40;
        public const int MAX_INTENSITY = 250;

        public const int DEFAULT_STROKE_WIDTH = 100;
        public const int MIN_STROKE_WIDTH = 0;
        public const int MAX_STROKE_WIDTH = 200;

        public const string DEFAULT_DIGITAL_SIGNATURE_COLOR = "#000000";
        public const string DEFAULT_DIGITAL_SIGNATURE_FONT = "Times New Roman";
        public const double DEFAULT_DIGITAL_SIGNATURE_FONT_SIZE = 10;
        public const double MIN_DIGITAL_SIGNATURE_FONT_SIZE = 6;
        public const double MAX_DIGITAL_SIGNATURE_FONT_SIZE = 24;

        public static readonly IReadOnlyList<string> DIGITAL_SIGNATURE_FONTS = [DEFAULT_DIGITAL_SIGNATURE_FONT, "Arial", "Verdana", "Georgia", "Courier New"];

        public const string COLOR_PATTERN = "^#[0-9A-Fa-f]{6}$";

        public const string IMAGE_OBJECT_KEY_PREFIX = "digital-signature/template";
        public const string STAMP_IMAGE_NAME = "stamp";
        public const string WET_SIGNATURE_IMAGE_NAME = "wet-signature";
        public static readonly IReadOnlyList<string> IMAGE_EXTENSIONS = [".png", ".jpg", ".jpeg"];
        public const long MAX_IMAGE_BYTES = 5 * 1024 * 1024;
    }
}
