import { BadgeTone, ITableBadge } from '@/shared/models/data-table.models';

// Khớp PromptTemplateConst của BE
export const PROMPT_TEMPLATE_STATUS = {
    DRAFT: 'DRAFT',
    PUBLISHED: 'PUBLISHED',
    ARCHIVED: 'ARCHIVED',
} as const;

export const PROMPT_TEMPLATE_STATUSES: { value: string; label: string; tone: BadgeTone }[] = [
    { value: PROMPT_TEMPLATE_STATUS.DRAFT, label: 'Bản nháp', tone: 'amber' },
    { value: PROMPT_TEMPLATE_STATUS.PUBLISHED, label: 'Đang dùng', tone: 'green' },
    { value: PROMPT_TEMPLATE_STATUS.ARCHIVED, label: 'Lưu trữ', tone: 'gray' },
];

// Nhãn trạng thái phiên bản cho StatusBadge; giá trị lạ hiện nguyên chữ kèm tone gray
export function getPromptTemplateStatusBadge(status: string): ITableBadge {
    const item = PROMPT_TEMPLATE_STATUSES.find((promptStatus) => promptStatus.value === status);
    return { label: item?.label ?? status, tone: item?.tone ?? 'gray' };
}

export const NORMALIZE_RULE = {
    NONE: 'NONE',
    TRIM: 'TRIM',
    DIGITS: 'DIGITS',
    NUMBER: 'NUMBER',
    DATE_DMY: 'DATE_DMY',
    UPPER: 'UPPER',
} as const;

export const NORMALIZE_RULES: { value: string; label: string }[] = [
    { value: NORMALIZE_RULE.NONE, label: 'Giữ nguyên' },
    { value: NORMALIZE_RULE.TRIM, label: 'Bỏ khoảng trắng thừa' },
    { value: NORMALIZE_RULE.DIGITS, label: 'Chỉ chữ số' },
    { value: NORMALIZE_RULE.NUMBER, label: 'Số' },
    { value: NORMALIZE_RULE.DATE_DMY, label: 'Ngày dd/MM/yyyy' },
    { value: NORMALIZE_RULE.UPPER, label: 'Chữ in hoa' },
];

export const PROMPT_MAX_LENGTH = 20000;
export const PROMPT_NOTE_MAX_LENGTH = 1000;
export const PROMPT_FIELD_DESCRIPTION_MAX_LENGTH = 2000;
export const PROMPT_FIELD_EXAMPLE_MAX_LENGTH = 500;
