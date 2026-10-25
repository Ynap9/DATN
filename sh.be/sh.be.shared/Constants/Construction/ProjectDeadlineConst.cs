namespace sh.be.shared.Constants.Construction
{
    public static class ProjectDeadlineConst
    {
        public const int NOTIFICATION_WARNING = 1;
        public const int NOTIFICATION_EXPIRED = 2;

        public static readonly IReadOnlyList<int> NOTIFICATION_TYPES = [NOTIFICATION_WARNING, NOTIFICATION_EXPIRED];

        public const string RECURRING_JOB_ID = "project-deadline-check";
        public const string LOCK_NAME = "project-deadline-check";
    }
}
