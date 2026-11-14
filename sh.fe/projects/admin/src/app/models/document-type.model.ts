import { IBaseRequestPaging } from '@/shared/models/request-paging.base.models';

export interface IFindPagingDocumentType extends IBaseRequestPaging {}

export interface IViewDocumentType {
    id: number;
    name: string;
    defaultView: number;
}

export interface IDocumentTypeDropdown {
    id: number;
    name: string;
}

export interface ICreateDocumentType {
    name: string;
    defaultView: number;
}

export interface IUpdateDocumentType {
    name: string;
    defaultView: number;
}
