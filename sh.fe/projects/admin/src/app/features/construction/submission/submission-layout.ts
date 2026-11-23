// Tuỳ chọn bố cục PDF / khu phải, lưu localStorage và dùng chung mọi dự án cho màn Nhập liệu và màn QA (43.4 / 43.14 / 44.4)
export interface ISubmissionLayout {
    direction: 'horizontal' | 'vertical';
    sizes: [number, number];
    fontSize: number;
}

export const FONT_SIZE_MIN = 12;
export const FONT_SIZE_MAX = 18;

const LAYOUT_STORAGE_KEY = 'construction.submission-check.layout';
const DEFAULT_LAYOUT: ISubmissionLayout = { direction: 'horizontal', sizes: [55, 45], fontSize: 14 };

// Đọc từng tuỳ chọn đã lưu; thiếu hoặc hỏng tuỳ chọn nào thì tuỳ chọn đó về mặc định
export function readSubmissionLayout(): ISubmissionLayout {
    try {
        const saved = JSON.parse(localStorage.getItem(LAYOUT_STORAGE_KEY) ?? '{}') as Partial<ISubmissionLayout>;
        const fontSize = saved.fontSize;
        return {
            direction: saved.direction === 'vertical' ? 'vertical' : 'horizontal',
            sizes: isValidSizes(saved.sizes) ? saved.sizes : DEFAULT_LAYOUT.sizes,
            fontSize: Number.isInteger(fontSize) && fontSize! >= FONT_SIZE_MIN && fontSize! <= FONT_SIZE_MAX ? fontSize! : DEFAULT_LAYOUT.fontSize,
        };
    } catch {
        return DEFAULT_LAYOUT;
    }
}

export function writeSubmissionLayout(layout: ISubmissionLayout) {
    try {
        localStorage.setItem(LAYOUT_STORAGE_KEY, JSON.stringify(layout));
    } catch {
        // localStorage bị chặn: chỉ nhớ bố cục tới khi tải lại trang
    }
}

function isValidSizes(sizes: unknown): sizes is [number, number] {
    return Array.isArray(sizes) && sizes.length === 2 && sizes.every((size) => typeof size === 'number' && size > 0);
}
