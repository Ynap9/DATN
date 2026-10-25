namespace sh.be.shared.Constants.Construction
{
    public static class ProjectConst
    {
        public const int DEPLOYING = 1;
        public const int DOING = 2;
        public const int ACCEPTING = 3;
        public const int DONE = 4;
        public const int EXPIRED = 5;

        public static readonly IReadOnlyList<int> STATUSES = [DEPLOYING, DOING, ACCEPTING, DONE, EXPIRED];
        public static readonly IReadOnlyList<int> ACTIVE_STATUSES = [DEPLOYING, DOING, ACCEPTING];
        public static readonly IReadOnlyList<int> DEADLINE_STATUSES = [DEPLOYING, DOING, ACCEPTING, EXPIRED];

        public const int DEFAULT_DEADLINE_WARNING_DAYS = 3;
    }
}
