import { IBaseRequestPaging } from '@/shared/models/request-paging.base.models';

export interface IFindPagingDocumentField extends IBaseRequestPaging {
    documentTypeId?: number;
}

export interface IViewDocumentField {
    id: number;
    documentTypeId: number;
    name: string;
    field: string;
    fieldType: string;
    fieldImport?: string;
    isShow: boolean;
    orderTableView: number;
    orderListView: number;
    isShowTableView: boolean;
    isShowListView: boolean;
    width?: number;
    isChargeable: boolean;
    fieldNameOcr?: string;
    isFieldOcr: boolean;
}

export interface IDocumentFieldDropdown {
    id: number;
    name: string;
}

export interface ICreateDocumentField {
    documentTypeId: number;
    name: string;
    field: string;
    fieldImport?: string;
    isShow: boolean;
    isChargeable: boolean;
    fieldNameOcr?: string;
    isFieldOcr: boolean;
}

export interface IUpdateDocumentField {
    name: string;
    field: string;
    fieldType: string;
    fieldImport?: string;
    isShow: boolean;
    order: number;
    isShowTableView: boolean;
    isShowListView: boolean;
    width?: number;
    isChargeable: boolean;
    fieldNameOcr?: string;
    isFieldOcr: boolean;
}

export interface IUpdateSettingDocumentField {
    isShow: boolean;
    isChargeable: boolean;
    isFieldOcr: boolean;
    fieldNameOcr?: string;
}

export interface IChangeOrderDocumentField {
    targetOrder: number;
}

export interface IQuickCreateDocumentField {
    documentTypeId: number;
    fieldNames: string;
}

export interface IQuickDeleteDocumentField {
    documentTypeId: number;
    ids: number[];
}

export interface ISheetHeader {
    sheetName: string;
    headers: string[];
}

export interface IMapFieldImport {
    documentTypeId: number;
    url: string;
    sheetName: string;
    headerStartIndex: number;
}

export interface IFieldImportHeader {
    fieldImport?: string;
    headers: string[];
}
