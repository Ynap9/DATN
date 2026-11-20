import { BadgeTone, ITableBadge, TagSeverity } from '@/shared/models/data-table.models';

export const PROJECT_URL = '/admin/construction/project';

export const PROJECT_STATUS = {
    DOING: 2,
    ACCEPTING: 3,
    DONE: 4,
    EXPIRED: 5,
} as const;

export const PROJECT_STATUSES: { value: number; label: string; severity: TagSeverity; tone: BadgeTone }[] = [
    { value: PROJECT_STATUS.DOING, label: 'Đang triển khai', severity: 'info', tone: 'blue' },
    { value: PROJECT_STATUS.ACCEPTING, label: 'Đang nghiệm thu', severity: 'contrast', tone: 'amber' },
    { value: PROJECT_STATUS.DONE, label: 'Đã xong', severity: 'success', tone: 'green' },
    { value: PROJECT_STATUS.EXPIRED, label: 'Hết hạn', severity: 'danger', tone: 'red' },
];

// Nhãn trạng thái dùng chung cho bảng và thẻ; giá trị lạ hiện số kèm tone gray
export function getProjectStatusBadge(status: number): ITableBadge {
    const item = PROJECT_STATUSES.find((projectStatus) => projectStatus.value === status);
    return { label: item?.label ?? String(status), tone: item?.tone ?? 'gray' };
}

export const ACCEPTANCE_REASON_MIN = 10;
export const ACCEPTANCE_REASON_MAX = 1000;
