import { IOutOfEnumField, IVerifyImportAppend, IVerifyImportBlank, IVerifyImportOverwrite } from '@/models/submission.model';
import { IMPORT_KIND, IMPORT_SOURCE, ImportKind, ImportSource, SUBMISSION_ERROR_CODE } from '@/shared/constants/submission.constants';

const NUMBER_FORMAT = new Intl.NumberFormat('vi-VN');

// Số kiểu vi-VN: 1.240
export function formatNumber(value: number) {
    return NUMBER_FORMAT.format(value);
}

// Lỗi báo bằng toast thay vì khung đỏ: 211 không có server chạy nền, 337 không còn quản lý dự án (48.18)
export const TOAST_ERROR_CODES: number[] = [SUBMISSION_ERROR_CODE.BACKGROUND_JOB_UNAVAILABLE, SUBMISSION_ERROR_CODE.PROJECT_NOT_MANAGED];

// Tên 4 bước wizard import (63.1); thứ tự = số bước
export const IMPORT_STEPS = ['Nguồn dữ liệu', 'Ghép trường', 'Kiểu import', 'Chạy thử & import'];

// Một trường ở bước Ghép trường (63.4): columnIndex = vị trí cột trong mảng header của sheet, null = chưa chọn cột
export interface IImportFieldRow {
    documentFieldId: number;
    name: string;
    columnIndex: number | null;
}

// Khoá so tên cột như BE (IsFieldImportMatch): không phân biệt hoa thường, gộp khoảng trắng
export function toHeaderKey(header: string) {
    return header.trim().replace(/\s+/g, ' ').toLocaleLowerCase('vi');
}

// Cột bắt đầu đọc mặc định, tính từ 1 (63.2)
export const IMPORT_DEFAULT_START_COLUMN = 1;

export const IMPORT_SOURCE_OPTIONS: { value: ImportSource; label: string; description: string; icon: string }[] = [
    { value: IMPORT_SOURCE.SHEET, label: 'Google Sheet', description: 'Dán link Google Sheet bất kỳ.', icon: 'pi pi-table' },
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
        description: 'Lấy cột đã ghép của một trường ghi đè lên văn bản đã có. Ô trống trên sheet: bỏ qua.',
    },
];

// Ô bước Kiểu import (63.6); cột trống gửi '', pageCountColumn chỉ dùng khi thêm mới, columnImport theo trường ghi đè
export interface IImportColumnMapping {
    fileColumnImport: string;
    linkColumnImport: string;
    profileRange: string;
    box: string;
    pageCountColumn: string;
    columnImport: string;
    documentFieldId: number | null;
    overwriteChecked: boolean;
}

export type ImportColumnKey = 'fileColumnImport' | 'linkColumnImport' | 'profileRange' | 'box' | 'pageCountColumn';

// Các ô cột cố định của bước Kiểu import; ô nào không còn trong header sheet mới thì xoá (63.10)
export const IMPORT_FIXED_COLUMN_KEYS: ImportColumnKey[] = ['fileColumnImport', 'linkColumnImport', 'profileRange', 'box', 'pageCountColumn'];

// Header trùng tên này (không phân biệt hoa thường) thì điền sẵn vào ô tương ứng
export const IMPORT_COLUMN_HINTS: Partial<Record<ImportColumnKey, string>> = {
    fileColumnImport: 'Tên file',
    linkColumnImport: 'Link',
    profileRange: 'Hồ sơ',
    box: 'Hộp',
    pageCountColumn: 'Số trang',
};

export function createColumnMapping(): IImportColumnMapping {
    return {
        fileColumnImport: '',
        linkColumnImport: '',
        profileRange: '',
        box: '',
        pageCountColumn: '',
        columnImport: '',
        documentFieldId: null,
        overwriteChecked: false,
    };
}

// Đủ ô * để chạy thử: tên file luôn bắt buộc, link khi thêm mới, ghi đè cần cột dữ liệu + trường
export function isColumnMappingComplete(mapping: IImportColumnMapping, kind: ImportKind) {
    if (!mapping.fileColumnImport.trim()) return false;
    if (kind === IMPORT_KIND.APPEND) return !!mapping.linkColumnImport.trim();
    if (kind !== IMPORT_KIND.OVERWRITE) return true;

    return !!mapping.columnImport.trim() && mapping.documentFieldId !== null;
}

// Dòng xám dưới số chạy thử (63.8): số BE trả chỉ đếm ô có dữ liệu
export const IMPORT_VERIFY_NOTE = 'Chỉ tính ô có dữ liệu; ô trống hoặc chỉ có khoảng trắng không tính.';

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
    // Trường kiểu danh mục có ô ngoài danh mục: chỉ báo, vẫn cho import (75.12)
    outOfEnum: IOutOfEnumField[];
}

export const IMPORT_JOB_RUNNING_MESSAGE: Record<ImportSource, string> = {
    [IMPORT_SOURCE.SHEET]: 'Đang import, có thể mất tới 1 phút. Đừng đóng trang.',
    [IMPORT_SOURCE.EXCEL]: 'Đã đưa vào hàng chờ chạy nền, đang import… Có thể rời trang, kết quả vẫn được ghi.',
};

export function getVerifyView(kind: ImportKind, result: IVerifyImportResult, hasPageCountColumn: boolean): IVerifyView {
    if (kind === IMPORT_KIND.APPEND) {
        const append = result as IVerifyImportAppend;
        return {
            main: { label: 'Văn bản sẽ tạo', value: append.totalAppendRowCount },
            subs: [
                { label: 'Ô có dữ liệu sẽ thêm', value: append.totalDataFields },
                { label: 'Trường đã ghép', value: append.totalFieldCount },
                { label: 'Văn bản đang có', value: append.totalSubmission },
                { label: 'Dòng trên sheet', value: append.totalSubmissionGoogleSheet },
                ...(hasPageCountColumn ? [{ label: 'Văn bản có số trang', value: append.totalPageCount }] : []),
            ],
            actionLabel: `Import ${formatNumber(append.totalAppendRowCount)} văn bản`,
            outOfEnum: append.outOfEnum,
        };
    }
    if (kind === IMPORT_KIND.BLANK) {
        const blank = result as IVerifyImportBlank;
        return {
            main: { label: 'Ô có dữ liệu sẽ điền vào chỗ trống', value: blank.totalBlankCount },
            subs: [{ label: 'Trường đã ghép', value: blank.totalFieldCount }],
            actionLabel: `Điền ${formatNumber(blank.totalBlankCount)} ô`,
            outOfEnum: blank.outOfEnum,
        };
    }
    const overwrite = result as IVerifyImportOverwrite;
    return {
        main: { label: 'Ô có dữ liệu sẽ ghi đè', value: overwrite.totalOverWriteCount },
        subs: [{ label: 'Văn bản đã kiểm bị bỏ qua', value: overwrite.totalSkippedChecked }],
        actionLabel: `Ghi đè ${formatNumber(overwrite.totalOverWriteCount)} ô`,
        outOfEnum: overwrite.outOfEnum,
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
    // Cờ lúc tạo job để hiện chip "Kèm gom hồ sơ" (BE không trả cờ)
    includeProfileSummary: boolean;
    status: number;
    downloadUrl: string | null;
    errorMessage: string | null;
    // Lúc nhận downloadUrl, quá EXPORT_DOWNLOAD_URL_MS thì phải lấy link mới
    readyAt: number | null;
}
