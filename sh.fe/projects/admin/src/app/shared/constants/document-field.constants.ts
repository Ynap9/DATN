export const DOCUMENT_FIELD_TYPE = {
    STRING: 'string',
    ENUM: 'enum',
} as const;

export const DOCUMENT_FIELD_TYPES = [
    { label: 'Chữ', value: DOCUMENT_FIELD_TYPE.STRING },
    { label: 'Danh mục', value: DOCUMENT_FIELD_TYPE.ENUM },
];

// Giới hạn danh mục khớp BE (DocumentFieldConst)
export const DOCUMENT_ENUM_NAME_MAX_LENGTH = 200;
export const DOCUMENT_ENUM_VALUES_MAX_COUNT = 500;
export const DOCUMENT_ENUM_VALUE_MAX_LENGTH = 500;

// Dòng chữ dưới ô khi giá trị nhập không thuộc danh mục (màn Nhập liệu / QA)
export const DOCUMENT_ENUM_OUT_OF_TEXT = 'Ngoài danh mục';

// Số giá trị hiện ở cột "Giá trị" của bảng danh mục, phần còn lại ghi "+n"
export const DOCUMENT_ENUM_PREVIEW_COUNT = 5;

export const DOCUMENT_ENUM_INPUT_MODE = {
    SINGLE: 'single',
    BULK: 'bulk',
} as const;

export const DOCUMENT_ENUM_INPUT_MODES = [
    { label: 'Từng giá trị', value: DOCUMENT_ENUM_INPUT_MODE.SINGLE },
    { label: 'Dán hàng loạt', value: DOCUMENT_ENUM_INPUT_MODE.BULK },
];
