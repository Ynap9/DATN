namespace sh.be.shared.Constants.Construction
{
    public static class ProjectDeadlineConst
    {
        public const int NOTIFICATION_WARNING = 1;
        public const int NOTIFICATION_EXPIRED = 2;
        // Dự án đang nghiệm thu đã quá ngày kết thúc: job chỉ báo mail, không đổi trạng thái
        public const int NOTIFICATION_ACCEPTING_OVERDUE = 3;

        public static readonly IReadOnlyList<int> NOTIFICATION_TYPES = [NOTIFICATION_WARNING, NOTIFICATION_EXPIRED, NOTIFICATION_ACCEPTING_OVERDUE];

        public const string RECURRING_JOB_ID = "project-deadline-check";
        public const string LOCK_NAME = "project-deadline-check";
    }
}
