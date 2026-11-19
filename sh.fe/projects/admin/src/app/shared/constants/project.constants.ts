import { TagSeverity } from '@/shared/models/data-table.models';

export const PROJECT_URL = '/admin/construction/project';

export const PROJECT_STATUS = {
    DEPLOYING: 1,
    DOING: 2,
    ACCEPTING: 3,
    DONE: 4,
    EXPIRED: 5,
} as const;

export const PROJECT_STATUSES: { value: number; label: string; severity: TagSeverity }[] = [
    { value: PROJECT_STATUS.DEPLOYING, label: 'Đang triển khai', severity: 'info' },
    { value: PROJECT_STATUS.DOING, label: 'Đang làm', severity: 'warn' },
    { value: PROJECT_STATUS.ACCEPTING, label: 'Đang nghiệm thu', severity: 'contrast' },
    { value: PROJECT_STATUS.DONE, label: 'Đã xong', severity: 'success' },
    { value: PROJECT_STATUS.EXPIRED, label: 'Hết hạn', severity: 'danger' },
];

export const ACCEPTANCE_REASON_MIN = 10;
export const ACCEPTANCE_REASON_MAX = 1000;
