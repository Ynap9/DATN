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
