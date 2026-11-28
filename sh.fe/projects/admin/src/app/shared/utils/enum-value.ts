// Giá trị khác rỗng và không thuộc danh mục của trường kiểu danh mục (so khớp sau trim, không phân biệt hoa thường)
export function isOutOfEnum(value: string, enumValues: string[] | null | undefined): boolean {
    const text = value.trim().toLowerCase();
    return !!enumValues && text.length > 0 && !enumValues.some((enumValue) => enumValue.toLowerCase() === text);
}
