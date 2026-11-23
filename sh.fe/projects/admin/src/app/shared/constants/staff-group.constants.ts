// Giới hạn nhóm nhân viên, khớp BE (StaffGroupConst / validation DTO)
export const STAFF_GROUP_NAME_MAX = 200;
export const STAFF_GROUP_DESCRIPTION_MAX = 1000;
export const STAFF_GROUP_MEMBERS_MAX = 500;

export const STAFF_GROUP_PAGE_SIZE = 20;
export const STAFF_GROUP_CANDIDATE_PAGE_SIZE = 20;

// Mã lỗi BE (ErrorCodes) màn nhóm nhân viên cần xử lý riêng
export const STAFF_GROUP_ERROR_CODE = {
    OPERATION_ERROR_STAFF_GROUP_NOT_FOUND: 801,
    OPERATION_ERROR_STAFF_GROUP_NAME_EXISTS: 802,
    OPERATION_ERROR_USER_INVALID_ROLE: 804,
} as const;
