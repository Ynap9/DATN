namespace sh.be.shared.Constants.Core
{
    public static class AuditLogConstants
    {
        public const string ACTION_TYPE_CREATE = "Create";
        public const string ACTION_TYPE_UPDATE = "Update";
        public const string ACTION_TYPE_DELETE = "Delete";
        public const string ACTION_TYPE_LOGIN = "Login";

        public const string ACTION_LOGIN = "Đăng nhập";
        public const string ACTION_LOGIN_FAILED = "Đăng nhập thất bại";

        public const string ACTION_USER_CREATE = "Tạo người dùng";
        public const string ACTION_USER_UPDATE = "Cập nhật người dùng";
        public const string ACTION_USER_DELETE = "Xóa người dùng";
        public const string ACTION_USER_CHANGE_PASSWORD = "Đổi mật khẩu";
        public const string ACTION_USER_UPDATE_ROLE = "Cập nhật vai trò của người dùng";

        public const string ACTION_ROLE_CREATE = "Tạo vai trò";
        public const string ACTION_ROLE_UPDATE = "Cập nhật vai trò";
        public const string ACTION_ROLE_DELETE = "Xóa vai trò";
        public const string ACTION_ROLE_UPDATE_PERMISSION = "Cập nhật quyền của vai trò";

        public const string ACTION_DOCUMENT_TYPE_CREATE = "Tạo loại văn bản";
        public const string ACTION_DOCUMENT_TYPE_UPDATE = "Cập nhật loại văn bản";
        public const string ACTION_DOCUMENT_TYPE_DELETE = "Xóa loại văn bản";

        public const string ACTION_SEED_ROLE = "Khởi tạo vai trò mặc định";
        public const string ACTION_SEED_PERMISSION = "Đồng bộ danh sách quyền";
        public const string ACTION_SEED_USER = "Khởi tạo tài khoản quản trị mặc định";
        public const string ACTION_SEED_ROLE_PERMISSION = "Khởi tạo quyền cho vai trò mặc định";

        public const string SOURCE_SYSTEM = "System";
    }
}
