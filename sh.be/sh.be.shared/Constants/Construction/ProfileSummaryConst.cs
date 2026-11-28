namespace sh.be.shared.Constants.Construction
{
    // Bảng gộp theo hồ sơ (mục lục hồ sơ theo TT05); nguồn: soHoaSaoMai projectSheetSync.service.js (GOM_FIELD_SOURCES)
    public static class ProfileSummaryConst
    {
        public const string SHEET_NAME = "Gom hồ sơ";

        public const string HEADER_BOX = "Hộp số";
        public const string HEADER_PROFILE = "Hồ sơ số";
        public const string HEADER_START_DATE = "Thời gian bắt đầu";
        public const string HEADER_END_DATE = "Thời gian kết thúc";
        public const string HEADER_TOTAL_SUBMISSION = "Tổng số văn bản";
        public const string HEADER_PAGE_COUNT = "Số trang";
        public static readonly List<string> FIXED_HEADERS = [HEADER_BOX, HEADER_PROFILE, HEADER_START_DATE, HEADER_END_DATE, HEADER_TOTAL_SUBMISSION, HEADER_PAGE_COUNT];
        // Cột cố định là số (canh phải); cột lấy từ trường cách "cộng" cũng là số
        public static readonly List<string> NUMBER_FIXED_HEADERS = [HEADER_TOTAL_SUBMISSION, HEADER_PAGE_COUNT];

        // Số trang không có ở PageCount thì cộng trường tên này
        public const string PAGE_COUNT_FIELD_NAME = "Số trang";
        // Trường có tên chứa một trong các cụm này (đã bỏ dấu, chữ thường) là trường ngày
        public static readonly List<string> DATE_FIELD_KEYWORDS = ["ngay", "thoi gian"];
        public const string DATE_FORMAT = "dd/MM/yyyy";
        public const int DATE_MIN_YEAR = 1900;
        public const int DATE_MAX_YEAR = 2100;

        public const string MERGE_COMMON = "common";
        public const string MERGE_SUM = "sum";
        public const string MERGE_JOIN = "join";
        public const string JOIN_SEPARATOR = "; ";

        // Cột lấy từ trường cùng tên (hoặc tên phụ), theo đúng thứ tự cột của bảng; "Ghi chú" không gộp nên không có ở đây
        public static readonly List<ProfileSummaryFieldColumn> FIELD_COLUMNS =
        [
            new("Mã hồ sơ", ["Mã HS"], MERGE_COMMON),
            new("Tiêu đề hồ sơ", ["Tên hồ sơ"], MERGE_COMMON),
            new("Thời hạn bảo quản", [], MERGE_COMMON),
            new("Chế độ sử dụng", [], MERGE_COMMON),
            new("Ngôn ngữ", [], MERGE_COMMON),
            new("Từ khoá", ["Từ khóa"], MERGE_JOIN),
            new("Số tờ", ["Số lượng tờ"], MERGE_SUM),
            new("Tình trạng vật lý", [], MERGE_COMMON),
            new("Ký hiệu thông tin", [], MERGE_COMMON),
            new("Mức độ tin cậy", [], MERGE_COMMON),
            new("Mã hồ sơ gốc giấy", [], MERGE_COMMON),
            new("Chế độ dự phòng", [], MERGE_COMMON),
            new("Tình trạng dự phòng", [], MERGE_COMMON),
        ];
    }

    public record ProfileSummaryFieldColumn(string Header, List<string> AliasNames, string MergeMethod);
}
