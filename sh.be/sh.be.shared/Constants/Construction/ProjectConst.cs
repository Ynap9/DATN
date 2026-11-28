namespace sh.be.shared.Constants.Construction
{
    public static class ProjectConst
    {
        public const int DOING = 2;
        public const int ACCEPTING = 3;
        public const int DONE = 4;
        public const int EXPIRED = 5;

        public static readonly IReadOnlyList<int> STATUSES = [DOING, ACCEPTING, DONE, EXPIRED];
        public static readonly IReadOnlyList<int> DEADLINE_STATUSES = [DOING, ACCEPTING, EXPIRED];
        // Dự án nhận trường mới của metadata; Đang nghiệm thu / Đã xong đã chốt nên bỏ qua (Task 74.7)
        public static readonly IReadOnlyList<int> NEW_FIELD_STATUSES = [DOING, EXPIRED];
        public static readonly IReadOnlyList<int> ACCEPTANCE_START_STATUSES = [DOING];

        public const int DEFAULT_DEADLINE_WARNING_DAYS = 3;

        public const int ACCEPTANCE_REASON_MIN = 10;
        public const int ACCEPTANCE_REASON_MAX = 1000;
    }
}
