import { IBaseRequestPaging } from '@/shared/models/request-paging.base.models';

export interface IFindPagingStaffGroup extends IBaseRequestPaging {
    ownerUserId?: string;
    role?: string;
}

// role: null = nhóm rỗng, "Data" / "QA", có dấu phẩy ("Data, QA") = nhóm lệch vai trò
export interface IViewStaffGroup {
    id: number;
    name: string;
    description: string | null;
    ownerUserId: string;
    ownerFullName: string | null;
    role: string | null;
    memberCount: number;
    createdDate: string | null;
}

// roleNames: mọi role hệ thống của người đó, nối ", " (vd "Data" hoặc "Data, Leader")
export interface IStaffGroupMember {
    userId: string;
    userName: string;
    fullName: string;
    email: string;
    roleNames: string;
    addedDate: string | null;
}

export interface IStaffGroupDetail {
    id: number;
    name: string;
    description: string | null;
    ownerUserId: string;
    ownerFullName: string | null;
    role: string | null;
    createdDate: string | null;
    members: IStaffGroupMember[];
}

export interface ICreateStaffGroup {
    name: string;
    description: string | null;
    userIds: string[];
}

export interface IUpdateStaffGroup {
    name: string;
    description: string | null;
}

export interface IChangeStaffGroupMembers {
    userIds: string[];
}

export interface IFindStaffGroupCandidate extends IBaseRequestPaging {
    role: string;
    excludeStaffGroupId?: number;
}

export interface IStaffGroupCandidate {
    userId: string;
    userName: string;
    fullName: string;
    email: string;
    role: string;
}

export interface IStaffGroupOwner {
    userId: string;
    userName: string;
    fullName: string;
    groupCount: number;
}
