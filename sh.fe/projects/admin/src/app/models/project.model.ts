import { IBaseRequestPaging } from '@/shared/models/request-paging.base.models';

export interface IFindPagingProject extends IBaseRequestPaging {
    status?: number;
    documentTypeId?: number;
    creatorKeyword?: string;
    // yyyy-MM-dd, BE lọc bao gồm hai đầu
    startedFrom?: string;
    startedTo?: string;
    endedFrom?: string;
    endedTo?: string;
}

export interface IViewProjectPair {
    projectId: number;
    projectName: string;
    status: number;
    startedAt: string | null;
    endedAt: string | null;
    isQaEnabled: boolean;
    urlGoogleSheetSync: string | null;
    creatorName?: string | null;
    documentTypeId: number;
    documentTypeName: string;
    viewerRole: string;
    memberCount: number;
    totalBox: number;
    totalProfileRange: number;
    totalSubmission: number;
    checkedCount: number;
    qaPassedCount: number | null;
    progressPercent: number;
    filledFieldCount: number | null;
}

export interface IProjectMyProgress {
    documentTypeId: number;
    documentTypeName: string;
    viewerRole: string;
    totalBox: number;
    totalProfileRange: number;
    totalSubmission: number;
    checkedCount: number;
    progressPercent: number;
    filledFieldCount: number | null;
}

export interface IViewProject {
    id: number;
    name: string;
    startedAt: string | null;
    endedAt: string | null;
    status: number;
    urlGoogleSheetSync: string | null;
    googleSheetName: string;
    isQaEnabled: boolean;
    documentTypeIds: number[];
    myProgresses: IProjectMyProgress[] | null;
}

export interface ICreateProject {
    name: string;
    startedAt: string | null;
    endedAt: string | null;
    isQaEnabled: boolean;
    documentTypeIds: number[];
}

export interface IUpdateProject {
    name: string;
    startedAt: string | null;
    endedAt: string | null;
    status: number;
    documentTypeIds: number[];
}

export interface IProjectAcceptanceSummary {
    documentTypeId: number | null;
    documentTypeName: string | null;
    isRemoved: boolean;
    totalSubmissions: number;
    totalNotChecked: number;
    totalChecked: number;
    totalReturned: number;
    totalQaNotReviewed: number;
    totalQaPassed: number;
    totalQaFailed: number;
    qaPassedPercent: number;
}

export interface IProjectAcceptanceTransition {
    action: string;
    createdDate: string;
    userId: string | null;
    userName: string | null;
    fullName: string | null;
    oldStatus: number | null;
    newStatus: number | null;
    isForced: boolean;
    isAutomatic: boolean;
    reason: string | null;
}

export interface IViewProjectAcceptance {
    projectId: number;
    projectName: string;
    status: number;
    isReady: boolean;
    canForce: boolean;
    documentTypes: IProjectAcceptanceSummary[];
    total: IProjectAcceptanceSummary;
    lastTransition: IProjectAcceptanceTransition | null;
}

export interface ITransitionAcceptance {
    isForced: boolean;
    reason?: string;
}

export interface IReopenAcceptance {
    reason: string;
}
