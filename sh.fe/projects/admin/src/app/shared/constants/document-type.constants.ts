export const METADATA_URL = '/admin/construction/project-config/metadata';

export const DOCUMENT_TYPE_VIEW_TABLE = 1;
export const DOCUMENT_TYPE_VIEW_LIST = 2;

export const DOCUMENT_TYPE_VIEWS: { value: number; label: string; icon: string; hint: string }[] = [
    {
        value: DOCUMENT_TYPE_VIEW_TABLE,
        label: 'Dạng bảng',
        icon: 'pi pi-table',
        hint: 'Dạng bảng: thứ tự cột khi nhập nhiều văn bản trên một bảng. Kéo biểu tượng ≡ để đổi thứ tự.',
    },
    {
        value: DOCUMENT_TYPE_VIEW_LIST,
        label: 'Dạng danh sách',
        icon: 'pi pi-list',
        hint: 'Dạng danh sách: thứ tự ô khi nhập từng văn bản, các trường xếp dọc. Kéo biểu tượng ≡ để đổi thứ tự.',
    },
];
