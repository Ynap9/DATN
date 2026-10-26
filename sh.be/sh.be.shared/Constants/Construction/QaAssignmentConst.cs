using sh.be.shared.Constants.Auth;

namespace sh.be.shared.Constants.Construction
{
    public static class QaAssignmentConst
    {
        public const string MODE_FULL = "FULL";
        public const string MODE_SAMPLE = "SAMPLE";
        public static readonly IReadOnlyList<string> MODES = [MODE_FULL, MODE_SAMPLE];
        public const int MODE_MAX_LENGTH = 10;

        public const int DEFAULT_SAMPLE_SIZE = 20;
        public const decimal DEFAULT_THRESHOLD_PERCENT = 50;
        public const string DEFAULT_STRATEGY = "SIMPLE_RANDOM";
        public const bool DEFAULT_STOP_EARLY = true;

        public const int SAMPLE_SIZE_MAX = 5000;
        public const decimal THRESHOLD_MAX = 100;
        public const string STRATEGY_CODE_PATTERN = "^[A-Z0-9_]{1,30}$";
        public const int STRATEGY_CODE_MAX_LENGTH = 30;
        public const int STRATEGY_PARAMS_MAX_LENGTH = 1000;

        public const string TICKET_STATUS_ACTIVE = "ACTIVE";
        public const string TICKET_STATUS_PASSED = "PASSED";
        public const string TICKET_STATUS_REJECTED = "REJECTED";
        public const string TICKET_STATUS_CANCELLED = "CANCELLED";
        public const int TICKET_STATUS_MAX_LENGTH = 20;
        public const int TICKET_MAX_DOCUMENTS = 5000;
        public const int UPDATE_CHUNK_SIZE = 1000;

        public static readonly IReadOnlyList<string> SUBMISSION_USER_ROLES = [RoleConstants.ROLE_DATA, RoleConstants.ROLE_QA];
        public const int SUBMISSION_USER_ROLE_MAX_LENGTH = 20;
    }
}
