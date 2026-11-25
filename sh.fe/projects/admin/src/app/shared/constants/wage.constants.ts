import { MEMBER_ROLE } from './project-member.constants';

export const WAGE_VIEW_MODE = {
    ADMIN: 'admin',
    LEADER: 'leader',
    STAFF: 'staff',
} as const;

export type WageViewMode = (typeof WAGE_VIEW_MODE)[keyof typeof WAGE_VIEW_MODE];

// Tab vai trò (chỉ admin / leader): key rỗng = không gửi role
export const WAGE_ROLE_TABS = [
    { key: '', label: 'Tất cả' },
    { key: MEMBER_ROLE.DATA, label: 'Nhập liệu' },
    { key: MEMBER_ROLE.QA, label: 'QA' },
];

export const WAGE_QUANTITY_UNITS: Record<string, string> = {
    [MEMBER_ROLE.DATA]: 'trường',
    [MEMBER_ROLE.QA]: 'lượt',
};

export const WAGE_PAGE_SIZE = 25;

export const WAGE_PAGE_SIZE_OPTIONS = [10, 25, 50, 100];

// Lựa chọn "Tất cả metadata" của ô Metadata: không gửi documentTypeId
export const WAGE_ALL_DOCUMENT_TYPES = 0;
