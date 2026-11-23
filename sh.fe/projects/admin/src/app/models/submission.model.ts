import { IBaseRequestPaging } from '@/shared/models/request-paging.base.models';

export interface ISubmissionFilter {
    projectId: number;
    documentTypeId: number;
    statusCheck?: number;
    statusQa?: number;
    keyword?: string;
    documentFieldId1?: number;
    fieldValue1?: string;
    isEmpty1?: boolean;
    documentFieldId2?: number;
    fieldValue2?: string;
    isEmpty2?: boolean;
}

export type IFindPagingSubmission = ISubmissionFilter & IBaseRequestPaging;

export interface ISubmissionData {
    id: number;
    documentFieldId: number;
    data: string;
    indexRow: number | null;
    indexGoogleSheet: string | null;
}

export interface IListSubmission {
    id: number;
    name: string;
    fileName: string;
    fileUrlS3Object: string;
    profileRange: string;
    box: string;
    statusCheck: number;
    statusQa: number;
    qaReason: string | null;
    createdDate: string | null;
    modifiedDate: string | null;
    submissionDatas: ISubmissionData[];
}

export interface ISubmissionColumn {
    documentFieldId: number;
    name: string;
    field: string;
    orderTableView: number;
    width: number | null;
}

export interface IListQaSubmission {
    id: number;
    name: string;
    fileName: string;
    fileUrlS3Object: string;
    profileRange: string;
    box: string;
    statusCheck: number;
    statusQa: number;
    qaReason: string | null;
    qaBy: string | null;
    qaUserName: string | null;
    qaDate: string | null;
    checkedBy: string | null;
    checkedUserName: string | null;
    checkedDate: string | null;
}

export interface ISubmissionCount {
    total: number;
    totalChecked: number;
    totalNotChecked: number;
    totalReturned: number;
}

export interface IQaCount {
    totalNotReviewed: number;
    totalPassed: number;
    totalFailed: number;
}

export type IFindSubmissionFieldValue = ISubmissionFilter & { documentFieldId: number };

export interface ISubmissionFieldValue {
    value: string;
    submissionCount: number;
}

export interface ILatestWorkingSubmission {
    submissionId: number;
    name: string;
    fileName: string;
    profileRange: string;
    box: string;
    statusCheck: number;
    statusQa: number;
    workedDate: string | null;
}

export type IFindSubmissionDetail = ISubmissionFilter & { includePosition?: boolean };

export interface ISubmissionField {
    documentFieldId: number;
    name: string;
    field: string;
    fieldType: string;
    isShow: boolean;
    isChargeable: boolean;
    orderListView: number;
    isShowListView: boolean;
    width: number | null;
    submissionDataId: number | null;
    data: string;
    indexGoogleSheet: string | null;
}

// position / totalItems chỉ có khi includePosition = true; position = null khi văn bản không thuộc bộ lọc
export interface ISubmissionDetail {
    id: number;
    projectId: number;
    documentTypeId: number;
    name: string;
    fileName: string;
    fileUrlS3Object: string;
    profileRange: string;
    box: string;
    statusCheck: number;
    statusQa: number;
    qaReason: string | null;
    qaWrongDocumentFieldIds: number[];
    createdDate: string | null;
    modifiedDate: string | null;
    modifiedBy: string | null;
    position: number | null;
    totalItems: number | null;
    fields: ISubmissionField[];
}

// GET {id}/qa: chi tiết văn bản kèm người chấm QA / người kiểm gần nhất (ViewQaSubmissionDetailDto)
export interface IQaSubmissionDetail extends ISubmissionDetail {
    qaBy: string | null;
    qaUserName: string | null;
    qaDate: string | null;
    checkedBy: string | null;
    checkedUserName: string | null;
    checkedDate: string | null;
    checkNote: string | null;
}

// result = QA_RESULT; Không đạt bắt buộc reason và ≥ 1 trường sai
export interface IReviewQaSubmission {
    id: number;
    projectId: number;
    documentTypeId: number;
    result: number;
    reason: string | null;
    wrongDocumentFieldIds: number[];
}

export interface IReviewQaResult {
    submissionId: number;
    statusCheck: number;
    statusQa: number;
    qaReason: string | null;
    wrongDocumentFieldIds: number[];
    qaLogId: number;
    qaTicketId: number | null;
    ticketStatus: string | null;
}

// GET {id}/qa-logs, mới nhất trước
export interface ISubmissionQaLog {
    id: number;
    submissionId: number;
    userId: string;
    userName: string | null;
    fullName: string | null;
    oldStatusQa: number;
    newStatusQa: number;
    oldStatusCheck: number;
    newStatusCheck: number;
    reason: string | null;
    wrongDocumentFieldIds: number[];
    description: string;
    createdDate: string | null;
}

export interface ICheckSubmissionData {
    documentFieldId: number;
    data: string;
}

export interface ICheckSubmission {
    id: number;
    projectId: number;
    documentTypeId: number;
    datas: ICheckSubmissionData[];
}

export interface ICheckSubmissionResult {
    submissionId: number;
    statusCheck: number;
    statusQa: number;
    changedDataCount: number;
    logId: number;
}

export interface ISubmissionTimelineField {
    documentFieldId: number;
    name: string;
}

export interface ISubmissionTimelineChangedField {
    documentFieldId: number;
    name: string;
    oldData: string;
    newData: string;
}

// eventType = TIMELINE_EVENT; trường CHECK / QA chỉ có giá trị ở sự kiện cùng loại. changedFields = null: log trước Task 43A
export interface ISubmissionTimeline {
    eventType: string;
    logId: number;
    createdDate: string | null;
    userId: string;
    userName: string | null;
    fullName: string | null;
    description: string;
    oldStatusCheck: number | null;
    newStatusCheck: number | null;
    note: string | null;
    changedDataCount: number | null;
    changedFields: ISubmissionTimelineChangedField[] | null;
    newStatusQa: number | null;
    reason: string | null;
    wrongFields: ISubmissionTimelineField[] | null;
    entryUserId: string | null;
    entryUserName: string | null;
}

// Lọc chung của các API thống kê; fromDate / toDate dạng yyyy-MM-dd, bỏ trống thì BE lấy khoảng mặc định
export interface IFindStats {
    projectId: number;
    documentTypeId: number;
    fromDate?: string;
    toDate?: string;
}

export type IFindPagingQaEntryUserStats = IFindStats & IBaseRequestPaging & { orderBy?: 'review' | 'accuracy' };

export interface IProgressDaily {
    date: string;
    checkedCount: number;
    qaPassedCount: number;
}

export interface IProgressDailyStats {
    fromDate: string;
    toDate: string;
    totalChecked: number;
    totalQaPassed: number;
    days: IProgressDaily[];
}

// Các tỷ lệ là phần trăm 0–100, BE làm tròn 2 số
export interface IQaStatsMetric {
    reviewCount: number;
    passedCount: number;
    failedCount: number;
    reviewedDocumentCount: number;
    firstReviewCount: number;
    firstPassCount: number;
    firstPassRate: number;
    wrongFieldCount: number;
    reviewedCellCount: number;
    fieldAccuracyRate: number;
    avgReturnPerDocument: number;
}

export type IQaStatsSummary = IQaStatsMetric & {
    fromDate: string;
    toDate: string;
    chargeableFieldCount: number;
};

export interface IQaFieldStats {
    documentFieldId: number;
    name: string;
    field: string;
    isConfigured: boolean;
    isChargeable: boolean;
    reviewCount: number;
    wrongCount: number;
    wrongRate: number;
    accuracyRate: number;
}

export type IQaEntryUserStats = IQaStatsMetric & {
    userId: string | null;
    userName: string | null;
    fullName: string | null;
};
