namespace sh.be.shared.Constants.Mail
{
    public static class MailTemplateConstants
    {
        public const string USERNAME = "{{UserName}}";
        public const string PASSWORD = "{{Password}}";
        public const string PROJECT_NAME = "{{ProjectName}}";
        public const string ENDED_AT = "{{EndedAt}}";
        public const string DEADLINE_STATUS = "{{DeadlineStatus}}";

        public const string SUBJECT_ACCOUNT_CREATED = "Thông tin tài khoản đăng nhập hệ thống";
        public const string SUBJECT_PROJECT_DEADLINE_WARNING = "Dự án {0} sắp hết hạn";
        public const string SUBJECT_PROJECT_EXPIRED = "Dự án {0} đã hết hạn";
    }
}
