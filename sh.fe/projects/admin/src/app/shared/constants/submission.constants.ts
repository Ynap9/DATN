import { TagSeverity } from '@/shared/models/data-table.models';

export const SUBMISSION_CHECK_STATUS = {
    NOT_CHECKED: 0,
    CHECKED: 1,
    RETURNED: 2,
} as const;

export const SUBMISSION_QA_STATUS = {
    NOT_REVIEWED: 0,
    PASSED: 1,
    FAILED: 2,
} as const;

// Nhãn + severity tag trạng thái (Task 42.17)
export const SUBMISSION_CHECK_STATUSES: { value: number; label: string; severity: TagSeverity }[] = [
    { value: SUBMISSION_CHECK_STATUS.NOT_CHECKED, label: 'Chưa kiểm', severity: 'secondary' },
    { value: SUBMISSION_CHECK_STATUS.CHECKED, label: 'Đã kiểm', severity: 'success' },
    { value: SUBMISSION_CHECK_STATUS.RETURNED, label: 'Trả lại', severity: 'danger' },
];

export const SUBMISSION_QA_STATUSES: { value: number; label: string; severity: TagSeverity }[] = [
    { value: SUBMISSION_QA_STATUS.NOT_REVIEWED, label: 'Chưa QA', severity: 'warn' },
    { value: SUBMISSION_QA_STATUS.PASSED, label: 'Đạt', severity: 'success' },
    { value: SUBMISSION_QA_STATUS.FAILED, label: 'Không đạt', severity: 'danger' },
];

export const LATEST_WORKING_TYPE = {
    CHECK: 'CHECK',
    QA: 'QA',
} as const;

export type LatestWorkingType = (typeof LATEST_WORKING_TYPE)[keyof typeof LATEST_WORKING_TYPE];

// Vai trò người xem trên màn danh sách văn bản, suy từ viewerRole BE (RoleConstants) của metadata
export const SUBMISSION_ROLE = {
    MANAGER: 'manager',
    DATA: 'data',
    QA: 'qa',
} as const;

export type SubmissionRole = (typeof SUBMISSION_ROLE)[keyof typeof SUBMISSION_ROLE];

export const SUBMISSION_ROLE_BY_VIEWER_ROLE: Record<string, SubmissionRole> = {
    Admin: SUBMISSION_ROLE.MANAGER,
    Leader: SUBMISSION_ROLE.MANAGER,
    Data: SUBMISSION_ROLE.DATA,
    QA: SUBMISSION_ROLE.QA,
};

export const SUBMISSION_ROLE_LABELS: Record<SubmissionRole, string> = {
    [SUBMISSION_ROLE.MANAGER]: 'Quản lý',
    [SUBMISSION_ROLE.DATA]: 'Nhập liệu',
    [SUBMISSION_ROLE.QA]: 'QA',
};

// Chế độ xem, giữ ở query ?view= (chế độ Nhập liệu là mặc định nên không ghi lên URL)
export const SUBMISSION_VIEW = {
    CHECK: 'check',
    QA: 'qa',
} as const;

export type SubmissionView = (typeof SUBMISSION_VIEW)[keyof typeof SUBMISSION_VIEW];

// key = tên trường số đếm (ISubmissionCount / IQaCount) của tab đó
export interface ISubmissionTab {
    key: string;
    label: string;
    statusCheck?: number;
    statusQa?: number;
}

export const SUBMISSION_CHECK_TABS: ISubmissionTab[] = [
    { key: 'total', label: 'Tất cả' },
    { key: 'totalNotChecked', label: 'Chưa kiểm', statusCheck: SUBMISSION_CHECK_STATUS.NOT_CHECKED },
    { key: 'totalChecked', label: 'Đã kiểm', statusCheck: SUBMISSION_CHECK_STATUS.CHECKED },
    { key: 'totalReturned', label: 'Trả lại', statusCheck: SUBMISSION_CHECK_STATUS.RETURNED },
];

// Chưa QA chỉ gồm văn bản đã kiểm: QA Không đạt đưa statusCheck về Trả lại (Submission.ReviewQa)
export const SUBMISSION_QA_TABS: ISubmissionTab[] = [
    { key: 'totalNotReviewed', label: 'Chưa QA', statusCheck: SUBMISSION_CHECK_STATUS.CHECKED, statusQa: SUBMISSION_QA_STATUS.NOT_REVIEWED },
    { key: 'totalPassed', label: 'Đạt', statusQa: SUBMISSION_QA_STATUS.PASSED },
    { key: 'totalFailed', label: 'Không đạt', statusQa: SUBMISSION_QA_STATUS.FAILED },
];

// Mã lỗi BE (ErrorCodes) màn danh sách (42.15), màn Nhập liệu (43.17 / 43.18) và màn QA (44.17 / 44.18) xử lý riêng
export const SUBMISSION_ERROR_CODE = {
    BACKGROUND_JOB_UNAVAILABLE: 211,
    DOCUMENT_TYPE_NOT_FOUND: 301,
    PROJECT_NOT_FOUND: 303,
    SUBMISSION_NOT_FOUND: 305,
    FIELD_NOT_IN_PROJECT: 311,
    DOCUMENT_TYPE_NOT_IN_PROJECT: 312,
    FILE_UNAVAILABLE: 315,
    PROJECT_DONE: 316,
    NO_MORE_SUBMISSION: 317,
    DUPLICATE_FIELD: 318,
    NOT_IN_DOCUMENT_TYPE: 319,
    NOT_CHECKED_FOR_QA: 320,
    QA_REASON_REQUIRED: 321,
    PROJECT_EXPIRED: 323,
    QA_WRONG_FIELD_REQUIRED: 324,
    CHECKED_NOT_DELETABLE: 325,
    QA_SUBMISSION_LOCKED: 332,
    QA_NOT_ASSIGNEE: 334,
    IN_PROGRESS_NOT_DELETABLE: 336,
    PROJECT_NOT_MANAGED: 337,
    PROJECT_QA_DISABLED: 345,
    NOT_ASSIGNED: 820,
    QA_PASSED_LOCKED: 826,
} as const;

// Kết quả chấm QA gửi lên PUT qa (SubmissionConst.QA_RESULTS)
export const QA_RESULT = {
    PASSED: 1,
    FAILED: 2,
} as const;

// SubmissionConst.QA_REASON_MAX_LENGTH
export const QA_REASON_MAX_LENGTH = 1000;

// Chip thêm nhanh vào lý do trả lại (44.15)
export const QA_QUICK_REASONS = ['Không khớp PDF', 'Sai chính tả', 'Thiếu dữ liệu', 'Sai định dạng ngày'];

// Loại sự kiện timeline văn bản (SubmissionConst.TIMELINE_EVENT_*)
export const TIMELINE_EVENT = {
    CHECK: 'CHECK',
    QA: 'QA',
} as const;

// Kiểu import = đoạn route BE: import/{kind}, import/excel/{kind} (Task 48)
export const IMPORT_KIND = {
    APPEND: 'append',
    BLANK: 'blank',
    OVERWRITE: 'overwrite',
} as const;

export type ImportKind = (typeof IMPORT_KIND)[keyof typeof IMPORT_KIND];

export const IMPORT_SOURCE = {
    SHEET: 'sheet',
    EXCEL: 'excel',
} as const;

export type ImportSource = (typeof IMPORT_SOURCE)[keyof typeof IMPORT_SOURCE];

// Trạng thái job chạy nền (BackgroundJobConst.STATUS_*)
export const BACKGROUND_JOB_STATUS = {
    WAITING: 0,
    RUNNING: 1,
    DONE: 2,
    FAILED: 3,
} as const;

// Cột thêm khi xuất Excel: mã = SubmissionConst.EXPORT_COLUMN_*, nhóm theo khối hộp tick (48.16)
export const EXPORT_COLUMN_GROUP = {
    COMMON: 'CHUNG',
    CHECK: 'KIỂM TRA',
    QA: 'QA',
    MODIFIED: 'SỬA CUỐI',
} as const;

export const EXPORT_COLUMNS: { value: string; label: string; group: string }[] = [
    { value: 'fileUrl', label: 'Đường dẫn file', group: EXPORT_COLUMN_GROUP.COMMON },
    { value: 'checkStatus', label: 'Trạng thái kiểm', group: EXPORT_COLUMN_GROUP.CHECK },
    { value: 'checkedBy', label: 'Người kiểm', group: EXPORT_COLUMN_GROUP.CHECK },
    { value: 'checkedDate', label: 'Thời gian kiểm', group: EXPORT_COLUMN_GROUP.CHECK },
    { value: 'qaStatus', label: 'Trạng thái QA', group: EXPORT_COLUMN_GROUP.QA },
    { value: 'qaBy', label: 'Người QA', group: EXPORT_COLUMN_GROUP.QA },
    { value: 'qaDate', label: 'Thời gian QA', group: EXPORT_COLUMN_GROUP.QA },
    { value: 'qaReason', label: 'Lý do QA', group: EXPORT_COLUMN_GROUP.QA },
    { value: 'modifiedBy', label: 'Người sửa cuối', group: EXPORT_COLUMN_GROUP.MODIFIED },
    { value: 'modifiedDate', label: 'Ngày sửa cuối', group: EXPORT_COLUMN_GROUP.MODIFIED },
];

// SubmissionConst.IMPORT_EXCEL_MAX_BYTES; FE chặn trước khi tải lên (48.7)
export const IMPORT_EXCEL_MAX_BYTES = 20 * 1024 * 1024;

// Chu kỳ hỏi trạng thái job import / xuất
export const JOB_POLL_MS = 2000;

// SubmissionConst.EXPORT_DOWNLOAD_URL_MINUTES: link tải file xuất hết hạn sau 15 phút
export const EXPORT_DOWNLOAD_URL_MS = 15 * 60 * 1000;
