import { IBaseRequestPaging } from '@/shared/models/request-paging.base.models';

export interface IFindPagingAuditLog extends IBaseRequestPaging {
    actionType?: string;
}

export interface IViewAuditLog {
    id: number;
    userId?: string;
    userName?: string;
    fullName?: string;
    action: string;
    actionType: string;
    source: string;
    contentType?: string;
    createdDate: string;
}

export interface IAuditLogDetail extends IViewAuditLog {
    content: string;
    oldData?: string;
    newData?: string;
}
