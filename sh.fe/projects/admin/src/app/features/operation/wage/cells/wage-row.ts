import { IGetListWage } from '@/models/wage.model';

// companyPrice: undefined = người xem không thấy giá công ty (hiện "—"), null = chưa đặt giá.
// documentTotal = số văn bản đã định dạng (Data: documentCount, QA: reviewDocumentCount).
// priceLink chỉ có ở chế độ Admin: nhãn "Chưa đặt giá" là link sang Thành viên dự án đúng cặp của dòng
export type WageRow = Omit<IGetListWage, 'companyPrice'> & {
    companyPrice: number | null | undefined;
    documentTotal: string;
    priceLink: { projectId: number; documentTypeId: number } | null;
};

const NUMBER_FORMAT = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 0 });

export function formatWageNumber(value: number): string {
    return NUMBER_FORMAT.format(value);
}
