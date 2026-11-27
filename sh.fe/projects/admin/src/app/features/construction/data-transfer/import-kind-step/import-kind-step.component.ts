import { Component, input, model } from '@angular/core';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { CheckboxModule } from 'primeng/checkbox';
import { ShSelectComponent } from '@sh/ui';
import { BaseComponent } from '@/shared/base/base.component';
import { IMPORT_KIND, ImportKind } from '@/shared/constants/submission.constants';
import { SharedImports } from '@/shared/import.shared';
import { IImportColumnMapping, IImportFieldRow, IMPORT_COLUMN_HINTS, IMPORT_KIND_OPTIONS, ImportColumnKey, toHeaderKey } from '../data-transfer.constants';

// Bước 3 wizard import: chọn kiểu import và các cột cố định (tên file, link, hồ sơ, hộp, số trang) hoặc trường bị ghi đè (63.6, 63.7)
@Component({
    selector: 'app-import-kind-step',
    imports: [SharedImports, AutoCompleteModule, CheckboxModule, ShSelectComponent],
    templateUrl: './import-kind-step.component.html',
})
export class ImportKindStepComponent extends BaseComponent {
    // Mọi trường đã ghép cột ở bước 2; ghi đè chỉ chọn được trong các trường này
    fieldRows = input.required<IImportFieldRow[]>();
    // Header đầy đủ của sheet đã chọn ở bước 1; columnIndex của trường là vị trí trong mảng này
    headers = input.required<string[]>();
    startColumn = input.required<number>();
    kind = model.required<ImportKind>();
    mapping = model.required<IImportColumnMapping>();

    readonly kindOptions = IMPORT_KIND_OPTIONS;
    readonly kindKey = IMPORT_KIND;

    suggestions: string[] = [];

    get isOverwrite() {
        return this.kind() === IMPORT_KIND.OVERWRITE;
    }

    // Cột chọn được chỉ từ "Bắt đầu đọc từ cột" trở đi, bỏ header rỗng
    private get selectableHeaders() {
        return this.headers()
            .slice(this.startColumn() - 1)
            .map((header) => header.trim())
            .filter(Boolean);
    }

    // Nhãn "<tên trường> ← <cột>", columnImport của BE chính là cột đã ghép của trường
    get fieldOptions() {
        return this.fieldRows().map((row) => ({ value: row.documentFieldId, label: `${row.name} ← ${this.getFieldHeader(row)}` }));
    }

    // Ô cột cố định theo kiểu import; link bắt buộc khi thêm mới, số trang chỉ khi thêm mới
    get columnFields(): { key: ImportColumnKey; label: string; required: boolean }[] {
        const isAppend = this.kind() === IMPORT_KIND.APPEND;
        return [
            { key: 'fileColumnImport', label: 'Cột tên file', required: true },
            ...(this.isOverwrite ? [] : [{ key: 'linkColumnImport' as const, label: 'Cột link file', required: isAppend }]),
            { key: 'profileRange', label: 'Cột Hồ sơ', required: false },
            { key: 'box', label: 'Cột Hộp', required: false },
            ...(isAppend ? [{ key: 'pageCountColumn' as const, label: 'Cột Số trang', required: false }] : []),
        ];
    }

    override ngOnInit(): void {
        this.applyHints();
        if (this.isOverwrite) this.selectOverwriteField(this.mapping().documentFieldId);
    }

    selectKind(kind: ImportKind) {
        this.kind.set(kind);
        this.applyHints();
        if (this.isOverwrite) this.selectOverwriteField(this.mapping().documentFieldId);
    }

    setValue<K extends keyof IImportColumnMapping>(key: K, value: IImportColumnMapping[K]) {
        this.mapping.update((mapping) => ({ ...mapping, [key]: value }));
    }

    // Chọn trường ghi đè: cột lấy dữ liệu theo cột đã ghép của trường (như 5sdb); chưa chọn / trường không còn thì chọn sẵn trường đầu
    selectOverwriteField(documentFieldId: number | null) {
        const row = this.fieldRows().find((item) => item.documentFieldId === documentFieldId) ?? this.fieldRows()[0];
        if (!row) return;
        this.mapping.update((mapping) => ({ ...mapping, documentFieldId: row.documentFieldId, columnImport: this.getFieldHeader(row) }));
    }

    searchHeaders(query: string) {
        const keyword = query.trim().toLocaleLowerCase('vi');
        this.suggestions = this.selectableHeaders.filter((header) => header.toLocaleLowerCase('vi').includes(keyword));
    }

    // Cột cố định trùng cột đã ghép cho trường nào thì nhắc, không chặn (63.7)
    getSharedFieldNote(key: ImportColumnKey) {
        const columnKey = toHeaderKey(this.mapping()[key]);
        if (!columnKey) return null;
        const names = this.fieldRows()
            .filter((row) => toHeaderKey(this.getFieldHeader(row)) === columnKey)
            .map((row) => row.name);
        return names.length ? `Cột ${this.mapping()[key].trim()} cũng đang ghép cho trường ${names.join(', ')}` : null;
    }

    private getFieldHeader(row: IImportFieldRow) {
        return this.headers()[row.columnIndex!].trim();
    }

    // Điền sẵn ô đang hiện còn trống bằng header trùng tên gợi ý, không phân biệt hoa thường
    private applyHints() {
        const headerByName = new Map(this.selectableHeaders.map((header) => [toHeaderKey(header), header]));
        const mapping = { ...this.mapping() };
        for (const { key } of this.columnFields) {
            const hint = IMPORT_COLUMN_HINTS[key];
            if (hint) mapping[key] ||= headerByName.get(toHeaderKey(hint)) ?? '';
        }
        this.mapping.set(mapping);
    }
}
