namespace sh.be.shared.Constants.Ai
{
    public static class AiModelConst
    {
        public const string CAPABILITY_VISION = "VISION";
        public const string CAPABILITY_PDF = "PDF";
        public const string CAPABILITY_JSON_MODE = "JSON_MODE";
        public static readonly IReadOnlyList<string> CAPABILITIES = [CAPABILITY_VISION, CAPABILITY_PDF, CAPABILITY_JSON_MODE];

        public const int CODE_MAX_LENGTH = 50;
        public const int DISPLAY_NAME_MAX_LENGTH = 200;
        public const int REMOTE_MODEL_NAME_MAX_LENGTH = 200;
        public const int CAPABILITIES_MAX_LENGTH = 100;
        public const int DEFAULT_PARAMS_JSON_MAX_LENGTH = 4000;
        public const int RESPONSE_MAPPING_JSON_MAX_LENGTH = 2000;
    }
}
