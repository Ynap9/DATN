import { IBaseRequestPaging } from '@/shared/models/request-paging.base.models';

export interface IAssignmentHolder {
    userId: string;
    fullName: string | null;
    documentCount: number;
}

// Một nút cây: cấp hộp có profileRange = null; box / profileRange "" = văn bản thiếu hộp / hồ sơ
export interface IAssignmentNode {
    box: string;
    profileRange: string | null;
    total: number;
    assigned: number;
    unassigned: number;
    checked: number;
    returned: number;
    qaPassed: number;
    notChecked: number;
    ownerUserId: string | null;
    ownerFullName: string | null;
    // holders chỉ ≤ 5 người, holderCount là tổng
    holderCount: number;
    holders: IAssignmentHolder[];
}

export interface IFindAssignmentTree extends IBaseRequestPaging {
    role: string;
    onlyUnassigned?: boolean;
    holderUserId?: string;
}

export interface IFindAssignmentProfiles extends IFindAssignmentTree {
    box: string;
}

// profileRange null = cả hộp (BE tự bung ra mọi hồ sơ)
export interface IAssignmentItem {
    box: string;
    profileRange: string | null;
}

export interface IAssignmentRequest {
    role: string;
    assigneeUserId: string;
    items: IAssignmentItem[];
}

export interface IAssignmentConflict {
    box: string;
    profileRange: string;
    holderUserId: string;
    holderFullName: string | null;
    documentCount: number;
    // true = xung đột vì người khác đang phụ trách nút (QA)
    isOwner: boolean;
}

export interface IAssignmentPreview {
    role: string;
    totalDocuments: number;
    newDocuments: number;
    alreadyHeldDocuments: number;
    conflictDocuments: number;
    laterDocuments: number;
    sameAsEntryDocuments: number;
    returnedToSameDocuments: number;
    conflicts: IAssignmentConflict[];
    normalizedItems: IAssignmentItem[];
    emptyItems: IAssignmentItem[];
    canSave: boolean;
    blockReasons: string[];
    warnings: string[];
}

export interface IAssignmentTicket {
    id: number;
    role: string;
    projectId: number;
    projectName: string | null;
    documentTypeId: number;
    documentTypeName: string | null;
    assigneeUserId: string;
    assigneeFullName: string | null;
    status: number;
    documentCount: number;
    createdBy: string | null;
    createdByFullName: string | null;
    createdDate: string | null;
    doneAt: string | null;
    items: IAssignmentItem[];
    total: number;
    checked: number;
    returned: number;
    qaPassed: number;
}
