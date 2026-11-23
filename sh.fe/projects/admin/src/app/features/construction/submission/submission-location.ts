// Dòng vị trí ở thanh trên màn Nhập liệu / QA: "Hồ sơ … · Hộp … · <metadata>", bỏ vế rỗng (43.3 / 53.3)
export function buildLocationText(profileRange: string | null | undefined, box: string | null | undefined, documentTypeName: string) {
    return [withPrefix('Hồ sơ', profileRange, ['hồ sơ', 'hs']), withPrefix('Hộp', box, ['hộp']), documentTypeName].filter(Boolean).join(' · ');
}

// Dữ liệu import có khi đã kèm tiền tố ("HS 114", "Hộp 12") — khi đó giữ nguyên, không thêm lần nữa
function withPrefix(prefix: string, value: string | null | undefined, aliases: string[]) {
    const text = value?.trim();
    if (!text) return '';
    const lower = text.toLocaleLowerCase('vi');
    return aliases.some((alias) => lower.startsWith(alias)) ? text : `${prefix} ${text}`;
}
