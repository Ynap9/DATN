import { IBaseRequestPaging } from '@/shared/models/request-paging.base.models';

export interface IFindPagingStaffGroup extends IBaseRequestPaging {
    ownerUserId?: string;
    role?: string;
}

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
