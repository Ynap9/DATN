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

        public const string ACTION_DOCUMENT_FIELD_CREATE = "Tạo trường của loại văn bản";
        public const string ACTION_DOCUMENT_FIELD_QUICK_CREATE = "Thêm nhanh trường của loại văn bản";
        public const string ACTION_DOCUMENT_FIELD_UPDATE = "Cập nhật trường của loại văn bản";
        public const string ACTION_DOCUMENT_FIELD_UPDATE_SETTING = "Cập nhật cấu hình trường của loại văn bản";
        public const string ACTION_DOCUMENT_FIELD_CHANGE_ORDER_TABLE_VIEW = "Cập nhật thứ tự hiển thị dạng bảng của trường";
        public const string ACTION_DOCUMENT_FIELD_CHANGE_ORDER_LIST_VIEW = "Cập nhật thứ tự hiển thị dạng danh sách của trường";
        public const string ACTION_DOCUMENT_FIELD_DELETE = "Xóa trường của loại văn bản";
        public const string ACTION_DOCUMENT_FIELD_QUICK_DELETE = "Xóa nhanh trường của loại văn bản";

        public const string ACTION_PROJECT_CREATE = "Tạo dự án";
        public const string ACTION_PROJECT_UPDATE = "Cập nhật dự án";
        public const string ACTION_PROJECT_DELETE = "Xóa dự án";
        public const string ACTION_PROJECT_EXPIRE = "Chuyển dự án sang hết hạn";
        public const string ACTION_PROJECT_DEADLINE_RUN = "Chạy kiểm tra hạn dự án";

        public const string ACTION_PROJECT_CONFIG_CREATE = "Tạo cấu hình dự án";
        public const string ACTION_PROJECT_CONFIG_UPDATE = "Cập nhật cấu hình dự án";
        public const string ACTION_PROJECT_CONFIG_DELETE = "Xóa cấu hình dự án";
        public const string ACTION_PROJECT_CONFIG_AUTHORIZE_GOOGLE = "Xác thực tài khoản Google cho cấu hình dự án";

        public const string ACTION_SUBMISSION_IMPORT_APPEND = "Import thêm mới hồ sơ từ Google Sheet";
        public const string ACTION_SUBMISSION_IMPORT_BLANK = "Import bổ sung dữ liệu trống từ Google Sheet";
        public const string ACTION_SUBMISSION_IMPORT_OVERWRITE = "Import ghi đè trường dữ liệu từ Google Sheet";
        public const string ACTION_SUBMISSION_CHECK = "Kiểm tra hồ sơ";
        public const string ACTION_SUBMISSION_QA = "QA hồ sơ";
        public const string ACTION_SUBMISSION_DELETE = "Xóa hồ sơ";

        public const string ACTION_QA_SAMPLING_CONFIG_UPDATE = "Cập nhật cấu hình chấm mẫu";
        public const string ACTION_QA_REVIEWER_MODE_UPDATE = "Đặt chế độ chấm QA";
        public const string ACTION_QA_TICKET_CREATE = "Giao phiếu QA";
        public const string ACTION_QA_TICKET_CONCLUDE = "Chốt phiếu QA";
        public const string ACTION_QA_TICKET_NEW_ROUND = "Bốc lại mẫu phiếu QA";
        public const string ACTION_QA_TICKET_CANCEL = "Huỷ phiếu QA";

        public const string ACTION_TEMPLATE_CREATE = "Tạo mẫu chữ ký";
        public const string ACTION_TEMPLATE_UPDATE = "Cập nhật mẫu chữ ký";
        public const string ACTION_TEMPLATE_UPDATE_CONFIG = "Cập nhật cấu hình mẫu chữ ký";
        public const string ACTION_TEMPLATE_DELETE = "Xóa mẫu chữ ký";

        public const string ACTION_SIGNING_BATCH_CREATE = "Tạo lô ký";
        public const string ACTION_SIGNING_BATCH_ADD_FILES = "Thêm file vào lô ký";
        public const string ACTION_SIGNING_BATCH_REMOVE_FILE = "Xóa file khỏi lô ký";
        public const string ACTION_SIGNING_BATCH_OPEN_SESSION = "Mở phiên ký";
        public const string ACTION_SIGNING_BATCH_START = "Bắt đầu ký lô";
        public const string ACTION_SIGNING_BATCH_PAUSE = "Tạm dừng lô ký";
        public const string ACTION_SIGNING_BATCH_CANCEL = "Hủy lô ký";

        public const string ACTION_STAFF_GROUP_CREATE = "Tạo nhóm nhân viên";
        public const string ACTION_STAFF_GROUP_UPDATE = "Cập nhật nhóm nhân viên";
        public const string ACTION_STAFF_GROUP_DELETE = "Xóa nhóm nhân viên";
        public const string ACTION_STAFF_GROUP_ADD_MEMBERS = "Thêm thành viên nhóm nhân viên";
        public const string ACTION_STAFF_GROUP_REMOVE_MEMBERS = "Bỏ thành viên nhóm nhân viên";

        public const string ACTION_PROJECT_MEMBER_ADD = "Thêm thành viên dự án";
        public const string ACTION_PROJECT_MEMBER_UPDATE = "Cập nhật thành viên dự án";
        public const string ACTION_PROJECT_MEMBER_REMOVE = "Gỡ thành viên dự án";

        public const string ACTION_PRICE_DEFAULT_UPDATE = "Cập nhật đơn giá mặc định dự án";
        public const string ACTION_MEMBER_PRICE_UPDATE = "Cập nhật đơn giá thành viên";

        public const string ACTION_ASSIGNMENT_CREATE = "Giao phiếu";
        public const string ACTION_ASSIGNMENT_REVOKE = "Thu hồi phiếu";
        public const string ACTION_ASSIGNMENT_TRANSFER = "Chuyển phiếu";
        public const string ACTION_ASSIGNMENT_RELEASE_TO_POOL = "Trả văn bản về pool";

        public const string ACTION_SEED_ROLE ="Khởi tạo vai trò mặc định";
        public const string ACTION_SEED_PERMISSION = "Đồng bộ danh sách quyền";
        public const string ACTION_SEED_USER = "Khởi tạo tài khoản quản trị mặc định";
        public const string ACTION_SEED_ROLE_PERMISSION = "Khởi tạo quyền cho vai trò mặc định";
        public const string ACTION_SEED_PACKAGING = "Khởi tạo cấu hình đóng gói";

        public const string SOURCE_SYSTEM = "System";
    }
}
