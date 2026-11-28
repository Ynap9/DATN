namespace sh.be.shared.Constants.Construction
{
    public static class DocumentFieldConst
    {
        public const string FIELD_TYPE_STRING = "string";
        public const string FIELD_TYPE_ENUM = "enum";
        public static readonly List<string> FIELD_TYPES = [FIELD_TYPE_STRING, FIELD_TYPE_ENUM];

        public const int ENUM_NAME_MAX_LENGTH = 200;
        public const int ENUM_VALUES_MAX_COUNT = 500;
        public const int ENUM_VALUE_MAX_LENGTH = 500;
        public const int ENUM_PROMPT_MAX_VALUES = 100;
        public const int ENUM_OUT_OF_ENUM_SAMPLE_COUNT = 5;
    }
}
