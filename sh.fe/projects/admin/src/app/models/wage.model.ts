import { IBaseRequestPaging, IBaseResponsePagingData } from '@/shared/models/request-paging.base.models';

export interface IFindWage extends IBaseRequestPaging {
    documentTypeId?: number;
    role?: string;
}

export interface IExportWage {
    documentTypeId?: number;
    role?: string;
    keyword?: string;
}

export interface IGetListWage {
    memberId: number;
    userId: string;
    userName: string;
    fullName: string | null;
    documentTypeId: number;
    documentTypeName: string;
    role: string;
    isActive: boolean;
    isRemoved: boolean;
    quantity: number;
    documentCount: number;
    reviewCount: number;
    reviewDocumentCount: number;
    passedCount: number;
    failedCount: number;
    companyPrice: number | null;
    actualPrice: number | null;
    isPriceMissing: boolean;
    actualAmount: number;
    companyAmount: number | null;
}

// Tổng trên toàn bộ dòng trong phạm vi người xem, không theo trang; companyAmount null = người xem không thấy giá công ty
export interface IViewWageSummary {
    dataQuantity: number;
    qaQuantity: number;
    actualAmount: number;
    companyAmount: number | null;
}

export type IViewWagePaging = IBaseResponsePagingData<IGetListWage> & {
    summary: IViewWageSummary;
};
