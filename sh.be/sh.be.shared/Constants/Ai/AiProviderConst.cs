namespace sh.be.shared.Constants.Ai
{
    public static class AiProviderConst
    {
        public const string PROTOCOL_OPENAI_COMPAT = "OPENAI_COMPAT";
        public const string PROTOCOL_ANTHROPIC = "ANTHROPIC";
        public const string PROTOCOL_GEMINI = "GEMINI";
        public const string PROTOCOL_CUSTOM_HTTP = "CUSTOM_HTTP";
        public static readonly IReadOnlyList<string> PROTOCOLS = [PROTOCOL_OPENAI_COMPAT, PROTOCOL_ANTHROPIC, PROTOCOL_GEMINI, PROTOCOL_CUSTOM_HTTP];
        public const int PROTOCOL_MAX_LENGTH = 30;

        public const string SCOPE_LOCAL = "LOCAL";
        public const string SCOPE_PUBLIC = "PUBLIC";
        public static readonly IReadOnlyList<string> SCOPES = [SCOPE_LOCAL, SCOPE_PUBLIC];
        public const int SCOPE_MAX_LENGTH = 10;

        public const string CODE_PATTERN = "^[a-z0-9][a-z0-9-]{1,49}$";
        public const int CODE_MAX_LENGTH = 50;
        public const int NAME_MAX_LENGTH = 200;
        public const int BASE_URL_MAX_LENGTH = 500;

        public const int TIMEOUT_DEFAULT = 120;
        public const int TIMEOUT_MIN = 5;
        public const int TIMEOUT_MAX = 600;
        public const int MAX_CONCURRENCY_DEFAULT = 2;
        public const int MAX_CONCURRENCY_MIN = 1;
        public const int MAX_CONCURRENCY_MAX = 32;
        public const int RATE_LIMIT_MIN = 1;
        public const int RATE_LIMIT_MAX = 10000;

        public const int EXTRA_HEADERS_MAX_ITEMS = 20;
        public const int EXTRA_HEADER_VALUE_MAX_LENGTH = 500;
        public const int EXTRA_HEADERS_JSON_MAX_LENGTH = 4000;
        public static readonly IReadOnlyList<string> FORBIDDEN_HEADERS =
        [
            "Authorization", "Proxy-Authorization", "X-Api-Key", "Api-Key", "X-Goog-Api-Key", "Cookie", "Host", "Content-Type", "Content-Length",
        ];

        public const int API_KEY_MAX_LENGTH = 1000;
        public const string API_KEY_PROTECTOR_PURPOSE = "sh.be.ai.provider-api-key";
        public const string API_KEY_HEADER_NAME_PATTERN = "^[A-Za-z0-9-]{1,100}$";
        public const int API_KEY_HEADER_NAME_MAX_LENGTH = 100;
        public const int API_KEY_HEADER_PREFIX_MAX_LENGTH = 20;
        public const int API_KEY_HASH_LENGTH = 64;
        public const int API_KEY_FINGERPRINT_LENGTH = 8;
        public const string API_KEY_FILE_TYPE = "ai_provider_api_key";
        public const string API_KEY_FILE_NAME_FORMAT = "{0}-api-key-{1:yyyyMMddHHmmss}.json";

        public const int MAX_PAGE_SIZE = 200;
    }
}
