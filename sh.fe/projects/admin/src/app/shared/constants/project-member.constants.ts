// Vai trò thành viên dự án = đúng tên role hệ thống BE (RoleConstants)
export const MEMBER_ROLE = {
    LEADER: 'Leader',
    DATA: 'Data',
    QA: 'QA',
} as const;

export type MemberRole = (typeof MEMBER_ROLE)[keyof typeof MEMBER_ROLE];

export const MEMBER_ROLE_LABELS: Record<string, string> = {
    [MEMBER_ROLE.LEADER]: 'Leader',
    [MEMBER_ROLE.DATA]: 'Nhập liệu',
    [MEMBER_ROLE.QA]: 'QA',
};

// viewerRole của cặp dự án – loại tài liệu mà người xem là Admin (gồm SuperAdmin)
export const MEMBER_VIEWER_ADMIN = 'Admin';

export const MEMBER_PRICE_MAX = 100000000;

export const MEMBER_PAGE_SIZE = 20;

// Mã lỗi BE (ErrorCodes) màn thành viên cần xử lý riêng
export const MEMBER_ERROR_CODE = {
    OPERATION_ERROR_NOT_PROJECT_LEADER: 805,
    OPERATION_ERROR_MEMBER_NOT_FOUND: 806,
    OPERATION_ERROR_PROJECT_CLOSED: 818,
} as const;
