using sh.be.shared.Constants.Auth;

namespace sh.be.shared.Constants.Operation
{
    public static class ProjectMemberConst
    {
        public static readonly IReadOnlyList<string> ROLES = [RoleConstants.ROLE_LEADER, RoleConstants.ROLE_DATA, RoleConstants.ROLE_QA];
        public const int ROLE_MAX_LENGTH = 20;

        public const int USERS_PER_REQUEST_MAX = 500;
        public const int MAX_PAGE_SIZE = 200;

        public const string SKIP_REASON_ALREADY_MEMBER = "Đã là thành viên của dự án / loại văn bản";
        public const string SKIP_REASON_INVALID_ROLE = "Không có vai trò hệ thống phù hợp";
        public const string SKIP_REASON_USER_NOT_FOUND = "Không tìm thấy người dùng";
    }
}
