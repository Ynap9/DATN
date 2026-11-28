import { ISheetHeader } from '@/models/document-field.model';
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
    holderUserName: string | null;
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
    enumValues: string[] | null;
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

export interface IDeleteSubmission {
    projectId: number;
    documentTypeId: number;
    ids: number[];
}

export interface IDeleteSubmissionResult {
    deletedSubmissionCount: number;
    deletedDataCount: number;
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

// Năng suất theo người (Task 70): userId null = "Không gắn người"; userName / fullName null = user đã xoá cứng
export interface IProgressUser {
    userId: string | null;
    userName: string | null;
    fullName: string | null;
    isRemoved: boolean;
    workingDays: number;
}

export interface IProgressDataUser extends IProgressUser {
    documentCount: number;
    fieldCount: number;
}

export interface IProgressQaUser extends IProgressUser {
    reviewCount: number;
    reviewDocumentCount: number;
    passedCount: number;
    failedCount: number;
}

export interface IProgressUserStats {
    fromDate: string;
    toDate: string;
    isQaEnabled: boolean;
    dataUsers: IProgressDataUser[];
    qaUsers: IProgressQaUser[];
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

// Body chung của 3 kiểu import, cả Google và Excel; ô cột trống gửi ''; startColumn tính từ 1
export interface IImportSubmission {
    projectId: number;
    documentTypeId: number;
    fileColumnImport: string;
    linkColumnImport: string;
    profileRange: string;
    box: string;
    pageCountColumn: string;
    startColumn: number;
}

// Body import Google Sheet: đọc đúng link + sheet người dùng gửi (63A), không đọc Sheet gắn với dự án
export interface IImportSheetSubmission extends IImportSubmission {
    url: string;
    sheetName: string;
}

export interface IImportOverwriteSubmission extends IImportSheetSubmission {
    columnImport: string;
    documentFieldId: number;
    overwriteChecked: boolean;
}

// Body import từ file Excel đã tải lên; overwrite không có url
export interface IImportExcelSubmission extends IImportSubmission {
    fileKey: string;
    sheetName: string;
}

export interface IImportExcelOverwriteSubmission extends IImportExcelSubmission {
    columnImport: string;
    documentFieldId: number;
    overwriteChecked: boolean;
}

// Query GET import/sheet-headers: tên sheet + header dòng 1 của link Google Sheet (63A)
export interface IImportSheetHeadersQuery {
    projectId: number;
    documentTypeId: number;
    url: string;
}

// Một trường của cặp dự án + loại văn bản và cột sheet BE gợi ý (match) hoặc vừa lưu (save, matchedHeader = '')
export interface IImportFieldMapping {
    documentFieldId: number;
    name: string;
    fieldImport: string;
    matchedHeader: string;
    matchedColumnIndex: number | null;
}

// columnIndex = vị trí cột trong mảng headers gửi lên (phân biệt các cột trùng tên)
export interface IImportFieldMappingChoice {
    documentFieldId: number;
    fieldImport: string;
    columnIndex: number;
}

export interface IMatchImportFieldMapping {
    projectId: number;
    documentTypeId: number;
    headers: string[];
    startColumn: number;
}

export interface ISaveImportFieldMapping extends IMatchImportFieldMapping {
    fields: IImportFieldMappingChoice[];
}

export interface IUploadImportExcelResult {
    fileKey: string;
    fileName: string;
    sheets: ISheetHeader[];
}

// Ô sắp ghi nằm ngoài danh mục của trường kiểu danh mục (chỉ báo, không chặn import)
export interface IOutOfEnumField {
    documentFieldId: number;
    name: string;
    count: number;
    samples: string[];
}

export interface IVerifyImportBase {
    outOfEnum: IOutOfEnumField[];
}

export interface IVerifyImportAppend extends IVerifyImportBase {
    totalDataFields: number;
    totalAppendRowCount: number;
    totalPageCount: number;
    totalFieldCount: number;
    totalSubmission: number;
    totalSubmissionGoogleSheet: number;
}

export interface IVerifyImportBlank extends IVerifyImportBase {
    totalBlankCount: number;
    totalFieldCount: number;
}

export interface IVerifyImportOverwrite extends IVerifyImportBase {
    totalOverWriteCount: number;
    totalSkippedChecked: number;
}

// Kết quả chạy thử theo kiểu import (khoá = IMPORT_KIND)
export interface IVerifyImportResultMap {
    append: IVerifyImportAppend;
    blank: IVerifyImportBlank;
    overwrite: IVerifyImportOverwrite;
}

export interface IImportSubmissionResult {
    message: string;
    totalDataFields: number;
}

// status theo BACKGROUND_JOB_STATUS
export interface ISubmissionImportJob {
    jobId: string;
    status: number;
    fileName: string;
    resultMessage: string | null;
    errorMessage: string | null;
}

// columns = mã cột thêm (EXPORT_COLUMNS), gửi lặp ?columns=a&columns=b
export type IExportSubmission = ISubmissionFilter & { columns?: string[] };

export interface ISubmissionExportJob {
    jobId: string;
    status: number;
    fileName: string;
    downloadUrl: string | null;
    errorMessage: string | null;
}

export type IExportSheetSubmission = IExportSubmission & { url: string; sheetName: string; startRow: number };

export interface IExportSheetNamesQuery {
    projectId: number;
    documentTypeId: number;
    url: string;
}

export interface ISubmissionExportSheetJob {
    jobId: string;
    status: number;
    errorMessage: string | null;
}
