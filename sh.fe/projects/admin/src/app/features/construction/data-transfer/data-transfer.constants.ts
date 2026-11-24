import { IVerifyImportAppend, IVerifyImportBlank, IVerifyImportOverwrite } from '@/models/submission.model';
import { IMPORT_KIND, IMPORT_SOURCE, ImportKind, ImportSource, SUBMISSION_ERROR_CODE } from '@/shared/constants/submission.constants';

const NUMBER_FORMAT = new Intl.NumberFormat('vi-VN');

// Số kiểu vi-VN: 1.240
export function formatNumber(value: number) {
    return NUMBER_FORMAT.format(value);
}

// Lỗi báo bằng toast thay vì khung đỏ: 211 không có server chạy nền, 337 không còn quản lý dự án (48.18)
export const TOAST_ERROR_CODES: number[] = [SUBMISSION_ERROR_CODE.BACKGROUND_JOB_UNAVAILABLE, SUBMISSION_ERROR_CODE.PROJECT_NOT_MANAGED];

// Tên 4 bước wizard import (48.5); thứ tự = số bước
export const IMPORT_STEPS = ['Nguồn dữ liệu', 'Kiểu import', 'Ghép cột', 'Chạy thử & import'];

export const IMPORT_SOURCE_OPTIONS: { value: ImportSource; label: string; description: string; icon: string }[] = [
    { value: IMPORT_SOURCE.SHEET, label: 'Google Sheet của dự án', description: 'Đọc sheet đã gắn khi tạo dự án.', icon: 'pi pi-table' },
    { value: IMPORT_SOURCE.EXCEL, label: 'File Excel (.xlsx)', description: 'Tải từ máy, import chạy nền.', icon: 'pi pi-upload' },
];

export const IMPORT_KIND_OPTIONS: { value: ImportKind; label: string; description: string }[] = [
    {
        value: IMPORT_KIND.APPEND,
        label: 'Thêm mới văn bản',
        description: 'Tạo văn bản chưa có (khớp theo Tên file + Hồ sơ + Hộp) kèm dữ liệu các trường. Văn bản đã có: bỏ qua.',
    },
    {
        value: IMPORT_KIND.BLANK,
        label: 'Bổ sung ô trống',
        description: 'Chỉ điền vào ô đang trống của văn bản đã có. Không tạo mới, không ghi đè ô có dữ liệu.',
    },
    {
        value: IMPORT_KIND.OVERWRITE,
        label: 'Ghi đè một trường',
        description: 'Lấy một cột trên sheet ghi đè một trường của văn bản đã có. Ô trống trên sheet: bỏ qua.',
    },
];

// Ô bước Ghép cột (48.10); cột trống gửi '', url / sheetName chỉ dùng khi Google + ghi đè
export interface IImportColumnMapping {
    fileColumnImport: string;
    linkColumnImport: string;
    profileRange: string;
    box: string;
    url: string;
    sheetName: string;
    columnImport: string;
    documentFieldId: number | null;
    overwriteChecked: boolean;
}

export type ImportColumnKey = 'fileColumnImport' | 'linkColumnImport' | 'profileRange' | 'box' | 'columnImport';

// Header trùng tên này (không phân biệt hoa thường) thì điền sẵn vào ô tương ứng
export const IMPORT_COLUMN_HINTS: Partial<Record<ImportColumnKey, string>> = {
    fileColumnImport: 'Tên file',
    linkColumnImport: 'Link',
    profileRange: 'Hồ sơ',
    box: 'Hộp',
};

export function createColumnMapping(url: string | null): IImportColumnMapping {
    return {
        fileColumnImport: '',
        linkColumnImport: '',
        profileRange: '',
        box: '',
        url: url ?? '',
        sheetName: '',
        columnImport: '',
        documentFieldId: null,
        overwriteChecked: false,
    };
}

// Đủ ô * để chạy thử: tên file luôn bắt buộc, link khi thêm mới, ghi đè cần cột dữ liệu + trường (Google thêm link + tên sheet)
export function isColumnMappingComplete(mapping: IImportColumnMapping, kind: ImportKind, source: ImportSource) {
    if (!mapping.fileColumnImport.trim()) return false;
    if (kind === IMPORT_KIND.APPEND) return !!mapping.linkColumnImport.trim();
    if (kind !== IMPORT_KIND.OVERWRITE) return true;

    const hasSheet = source !== IMPORT_SOURCE.SHEET || (!!mapping.url.trim() && !!mapping.sheetName.trim());
    return hasSheet && !!mapping.columnImport.trim() && mapping.documentFieldId !== null;
}

export type IVerifyImportResult = IVerifyImportAppend | IVerifyImportBlank | IVerifyImportOverwrite;

export interface IVerifyMetric {
    label: string;
    value: number;
}

// Kết quả chạy thử hiển thị ở bước 4 (48.11): ô số chính, các ô số phụ, chữ nút import theo số chính
export interface IVerifyView {
    main: IVerifyMetric;
    subs: IVerifyMetric[];
    actionLabel: string;
}

export const IMPORT_JOB_RUNNING_MESSAGE: Record<ImportSource, string> = {
    [IMPORT_SOURCE.SHEET]: 'Đang import, có thể mất tới 1 phút. Đừng đóng trang.',
    [IMPORT_SOURCE.EXCEL]: 'Đã đưa vào hàng chờ chạy nền, đang import… Có thể rời trang, kết quả vẫn được ghi.',
};

export function getVerifyView(kind: ImportKind, result: IVerifyImportResult): IVerifyView {
    if (kind === IMPORT_KIND.APPEND) {
        const append = result as IVerifyImportAppend;
        return {
            main: { label: 'Văn bản sẽ tạo', value: append.totalAppendRowCount },
            subs: [
                { label: 'Ô có dữ liệu sẽ thêm', value: append.totalDataFields },
                { label: 'Trường đã ghép', value: append.totalFieldCount },
                { label: 'Văn bản đang có', value: append.totalSubmission },
                { label: 'Dòng trên sheet', value: append.totalSubmissionGoogleSheet },
            ],
            actionLabel: `Import ${formatNumber(append.totalAppendRowCount)} văn bản`,
        };
    }
    if (kind === IMPORT_KIND.BLANK) {
        const blank = result as IVerifyImportBlank;
        return {
            main: { label: 'Ô trống sẽ được điền', value: blank.totalBlankCount },
            subs: [{ label: 'Trường đã ghép', value: blank.totalFieldCount }],
            actionLabel: `Điền ${formatNumber(blank.totalBlankCount)} ô`,
        };
    }
    const overwrite = result as IVerifyImportOverwrite;
    return {
        main: { label: 'Ô sẽ bị ghi đè', value: overwrite.totalOverWriteCount },
        subs: [{ label: 'Văn bản đã kiểm bị bỏ qua', value: overwrite.totalSkippedChecked }],
        actionLabel: `Ghi đè ${formatNumber(overwrite.totalOverWriteCount)} ô`,
    };
}

// Cột tick sẵn khi mở tab Xuất (48.16)
export const EXPORT_DEFAULT_COLUMNS = ['checkStatus', 'checkedBy'];

// Đích xuất của tab Xuất dữ liệu (56.2); thẻ chọn đích cùng khuôn thẻ nguồn ở bước 1 tab Nhập
export const EXPORT_TARGET = {
    EXCEL: 'excel',
    SHEET: 'sheet',
} as const;

export type ExportTarget = (typeof EXPORT_TARGET)[keyof typeof EXPORT_TARGET];

export const EXPORT_TARGET_OPTIONS: { value: ExportTarget; label: string; description: string; icon: string }[] = [
    { value: EXPORT_TARGET.EXCEL, label: 'File Excel (.xlsx)', description: 'Tạo file chạy nền, tải về máy.', icon: 'pi pi-file-excel' },
    { value: EXPORT_TARGET.SHEET, label: 'Google Sheet', description: 'Ghi vào một sheet có sẵn, thay dữ liệu từ dòng bắt đầu.', icon: 'pi pi-table' },
];

// Một dòng "Đã xuất": chỉ giữ trong phiên của trang, BE không có API liệt kê job (48.17, 56.11)
// Dòng Excel dùng fileName / downloadUrl / readyAt; dòng Sheet dùng sheetUrl / sheetName / startRow / total (fileName để '')
export interface IExportJobRow {
    jobId: string;
    target: ExportTarget;
    fileName: string;
    sheetUrl?: string;
    sheetName?: string;
    startRow?: number;
    total?: number;
    status: number;
    downloadUrl: string | null;
    errorMessage: string | null;
    // Lúc nhận downloadUrl, quá EXPORT_DOWNLOAD_URL_MS thì phải lấy link mới
    readyAt: number | null;
}
