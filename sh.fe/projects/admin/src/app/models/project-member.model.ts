import { IBaseRequestPaging } from '@/shared/models/request-paging.base.models';

export interface IFindPagingProjectMember extends IBaseRequestPaging {
    documentTypeId: number;
    role?: string;
    managedByUserId?: string;
    isActive?: boolean;
}

export interface IViewProjectMember {
    id: number;
    projectId: number;
    documentTypeId: number;
    userId: string;
    userName: string;
    fullName: string | null;
    role: string;
    managedByUserId: string | null;
    managedByFullName: string | null;
    sourceStaffGroupId: number | null;
    sourceStaffGroupName: string | null;
    isActive: boolean;
    companyPrice: number | null;
    actualPrice: number | null;
    createdDate: string | null;
    heldTicketCount: number;
}

export interface IFindMemberCandidate extends IBaseRequestPaging {
    documentTypeId: number;
    role: string;
    staffGroupId?: number;
}

export interface IMemberCandidate {
    userId: string;
    userName: string;
    fullName: string | null;
    email: string;
}

export interface IAddProjectLeader {
    documentTypeId: number;
    userIds: string[];
}

export interface IAddMemberItem {
    userId: string;
    role: string;
}

export interface IAddProjectMember {
    documentTypeId: number;
    managedByUserId?: string;
    users: IAddMemberItem[];
    staffGroupIds: number[];
}

export interface ISkippedMember {
    userId: string;
    fullName: string | null;
    reason: string;
}

export interface IAddProjectMemberResult {
    addedCount: number;
    added: IViewProjectMember[];
    skipped: ISkippedMember[];
}

export interface IUpdateMemberPriceItem {
    memberId: number;
    companyPrice?: number;
    actualPrice?: number;
}

export interface IUpdateMemberPrices {
    items: IUpdateMemberPriceItem[];
}

export interface IUpdateMemberPricesResult {
    updatedCount: number;
    members: IViewProjectMember[];
}

export interface IUpdateProjectMember {
    managedByUserId?: string;
    clearManager: boolean;
    isActive?: boolean;
}

export interface IRemoveProjectMemberResult {
    revokedDocumentCount: number;
    keptDocumentCount: number;
    cancelledAssignmentTicketIds: number[];
    cancelledQaTicketIds: number[];
    unmanagedMemberCount: number;
}
