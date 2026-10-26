namespace sh.be.shared.Constants.Ai
{
    public static class PromptTemplateConst
    {
        public const string STATUS_DRAFT = "DRAFT";
        public const string STATUS_PUBLISHED = "PUBLISHED";
        public const string STATUS_ARCHIVED = "ARCHIVED";
        public const int STATUS_MAX_LENGTH = 20;
        public const int NOTE_MAX_LENGTH = 1000;

        public const string NORMALIZE_NONE = "NONE";
        public const string NORMALIZE_TRIM = "TRIM";
        public const string NORMALIZE_DIGITS = "DIGITS";
        public const string NORMALIZE_NUMBER = "NUMBER";
        public const string NORMALIZE_DATE_DMY = "DATE_DMY";
        public const string NORMALIZE_UPPER = "UPPER";
        public const int NORMALIZE_RULE_MAX_LENGTH = 30;

        public const int OUTPUT_KEY_MAX_LENGTH = 100;
        public const int FIELD_DESCRIPTION_MAX_LENGTH = 2000;
        public const int FIELD_EXAMPLE_MAX_LENGTH = 500;
    }
}
