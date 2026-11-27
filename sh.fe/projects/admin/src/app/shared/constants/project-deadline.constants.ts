import { ITableBadge, TagSeverity } from '@/shared/models/data-table.models';

export const PROJECT_DEADLINE_URL = '/admin/construction/project-deadline';

// Khớp loại báo hạn của BE: dự án sắp hết hạn / đã hết hạn
export const DEADLINE_TYPE = {
    SOON: 1,
    EXPIRED: 2,
} as const;

// Trạng thái lần chạy gần nhất của job kiểm hạn do Hangfire trả (lastJobState)
export const DEADLINE_JOB_STATE = {
    SUCCEEDED: 'Succeeded',
    FAILED: 'Failed',
    PROCESSING: 'Processing',
    ENQUEUED: 'Enqueued',
} as const;

const DEADLINE_RUNNING_JOB_STATES: string[] = [DEADLINE_JOB_STATE.PROCESSING, DEADLINE_JOB_STATE.ENQUEUED];

// "Kiểm hạn ngay": hỏi lại trạng thái job mỗi 2 giây, tối đa 15 lần
export const DEADLINE_POLL_INTERVAL_MS = 2000;
export const DEADLINE_POLL_MAX_TIMES = 15;

export function isDeadlineJobRunning(state: string | null) {
    return !!state && DEADLINE_RUNNING_JOB_STATES.includes(state);
}

// Nhãn tag lần chạy gần nhất; null (chưa chạy lần nào) và giá trị lạ hiện "Chưa chạy"
export function getDeadlineJobStateTag(state: string | null): { label: string; severity: TagSeverity } {
    if (state === DEADLINE_JOB_STATE.SUCCEEDED) return { label: 'Thành công', severity: 'success' };
    if (state === DEADLINE_JOB_STATE.FAILED) return { label: 'Lỗi', severity: 'danger' };
    if (isDeadlineJobRunning(state)) return { label: 'Đang chạy', severity: 'info' };
    return { label: 'Chưa chạy', severity: 'secondary' };
}

// Hai tab của màn Hạn dự án
export const DEADLINE_TAB = {
    ATTENTION: 'attention',
    MAILS: 'mails',
} as const;

export type DeadlineTab = (typeof DEADLINE_TAB)[keyof typeof DEADLINE_TAB];

export const DEADLINE_TABS: { key: DeadlineTab; label: string }[] = [
    { key: DEADLINE_TAB.ATTENTION, label: 'Dự án cần chú ý' },
    { key: DEADLINE_TAB.MAILS, label: 'Lịch sử mail' },
];

export const DEADLINE_PAGE_SIZE = 25;
export const DEADLINE_SEARCH_DEBOUNCE_MS = 400;

// Lọc loại báo hạn: 0 = cả hai (BE bỏ trống notificationType)
export const DEADLINE_TYPE_ALL = 0;

export const DEADLINE_TYPE_FILTERS = [
    { label: 'Tất cả', value: DEADLINE_TYPE_ALL },
    { label: 'Sắp hết hạn', value: DEADLINE_TYPE.SOON },
    { label: 'Đã hết hạn', value: DEADLINE_TYPE.EXPIRED },
];

// Lọc kết quả gửi mail; isSuccess của BE: bỏ trống / true / false
export const DEADLINE_RESULT_FILTER = {
    ALL: 'all',
    SUCCESS: 'success',
    FAILED: 'failed',
} as const;

export type DeadlineResultFilter = (typeof DEADLINE_RESULT_FILTER)[keyof typeof DEADLINE_RESULT_FILTER];

export const DEADLINE_RESULT_FILTERS = [
    { label: 'Tất cả', value: DEADLINE_RESULT_FILTER.ALL },
    { label: 'Thành công', value: DEADLINE_RESULT_FILTER.SUCCESS },
    { label: 'Lỗi', value: DEADLINE_RESULT_FILTER.FAILED },
];

export function toIsSuccess(filter: DeadlineResultFilter) {
    if (filter === DEADLINE_RESULT_FILTER.ALL) return undefined;
    return filter === DEADLINE_RESULT_FILTER.SUCCESS;
}

export function getDeadlineTypeBadge(type: number): ITableBadge {
    return type === DEADLINE_TYPE.EXPIRED ? { label: 'Đã hết hạn', tone: 'red' } : { label: 'Sắp hết hạn', tone: 'amber' };
}

// Còn n ngày / hết hạn hôm nay: amber; quá hạn: red
export function getDaysLeftBadge(daysLeft: number): ITableBadge {
    if (daysLeft > 0) return { label: `Còn ${daysLeft} ngày`, tone: 'amber' };
    if (daysLeft === 0) return { label: 'Hết hạn hôm nay', tone: 'amber' };
    return { label: `Quá ${-daysLeft} ngày`, tone: 'red' };
}
