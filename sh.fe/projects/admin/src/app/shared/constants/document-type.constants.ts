export const METADATA_URL = '/admin/construction/metadata';

// Tab của màn chi tiết Metadata, giữ ở query ?tab= (tab Trường là mặc định nên không ghi lên URL)
export const METADATA_TAB = {
    FIELDS: 'fields',
    PROMPT: 'prompt',
} as const;

export const DOCUMENT_TYPE_VIEW_TABLE = 1;
export const DOCUMENT_TYPE_VIEW_LIST = 2;

export const DOCUMENT_TYPE_VIEWS: { value: number; label: string; icon: string; description: string[] }[] = [
    {
        value: DOCUMENT_TYPE_VIEW_TABLE,
        label: 'Dạng bảng',
        icon: 'pi pi-table',
        description: [
            'Nhập nhiều văn bản trên một lưới, mỗi dòng là một văn bản.',
            'Mỗi trường là một cột, xếp từ trái sang phải theo thứ tự này.',
            'Hợp với trường ngắn, cần nhập nhanh hàng loạt.',
        ],
    },
    {
        value: DOCUMENT_TYPE_VIEW_LIST,
        label: 'Dạng danh sách',
        icon: 'pi pi-list',
        description: [
            'Nhập từng văn bản một: PDF bên trái, form bên phải.',
            'Các trường xếp từ trên xuống theo thứ tự này.',
            'Hợp với trường dài như trích yếu, cần đối chiếu kỹ với PDF.',
        ],
    },
];
