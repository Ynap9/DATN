import { Component, DestroyRef, inject, input, model } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { CheckboxModule } from 'primeng/checkbox';
import { ShSelectComponent } from '@sh/ui';
import { IDocumentFieldDropdown, ISheetHeader } from '@/models/document-field.model';
import { DocumentFieldService } from '@/services/document-field.service';
import { SharedService } from '@/services/shared.service';
import { BaseComponent } from '@/shared/base/base.component';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { IMPORT_KIND, IMPORT_SOURCE, ImportKind, ImportSource } from '@/shared/constants/submission.constants';
import { SharedImports } from '@/shared/import.shared';
import { IImportColumnMapping, IMPORT_COLUMN_HINTS, ImportColumnKey } from '../data-transfer.constants';

// Bước 3 wizard import: ghép cột trên sheet vào ô của API import (48.9, 48.10); component tạo mới mỗi lần vào bước
@Component({
    selector: 'app-import-column-step',
    imports: [SharedImports, AutoCompleteModule, CheckboxModule, ShSelectComponent],
    templateUrl: './import-column-step.component.html',
})
export class ImportColumnStepComponent extends BaseComponent {
    private readonly _documentFieldService = inject(DocumentFieldService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);

    kind = input.required<ImportKind>();
    source = input.required<ImportSource>();
    documentTypeId = input.required<number>();
    urlGoogleSheetSync = input<string | null>(null);
    googleSheetName = input<string | null>(null);
    // Header của sheet Excel đã chọn ở bước 1
    excelHeaders = input<string[]>([]);
    mapping = model.required<IImportColumnMapping>();

    readonly kindKey = IMPORT_KIND;
    readonly sourceKey = IMPORT_SOURCE;

    documentFields: IDocumentFieldDropdown[] = [];
    suggestions: string[] = [];
    // false = không lấy được tên cột (thiếu quyền / lỗi), các ô vẫn gõ tay được
    canReadHeaders = true;
    private _googleSheets: ISheetHeader[] = [];
    private _loadedUrl: string | null = null;

    get isOverwrite() {
        return this.kind() === IMPORT_KIND.OVERWRITE;
    }

    get headers() {
        if (this.source() === IMPORT_SOURCE.EXCEL) return this.excelHeaders();
        const sheetName = this.isOverwrite ? this.mapping().sheetName : this.googleSheetName();
        return this._googleSheets.find((sheet) => sheet.sheetName === sheetName)?.headers ?? [];
    }

    // Ô chọn cột theo kiểu import; link bắt buộc khi thêm mới, bổ sung ô trống được bỏ trống
    get columnFields(): { key: ImportColumnKey; label: string; required: boolean }[] {
        const linkField = { key: 'linkColumnImport' as const, label: 'Cột link file', required: this.kind() === IMPORT_KIND.APPEND };
        return [
            { key: 'fileColumnImport', label: 'Cột tên file', required: true },
            ...(this.isOverwrite ? [] : [linkField]),
            { key: 'profileRange', label: 'Cột Hồ sơ', required: false },
            { key: 'box', label: 'Cột Hộp', required: false },
            ...(this.kind() === IMPORT_KIND.APPEND ? [{ key: 'pageCountColumn' as const, label: 'Cột Số trang', required: false }] : []),
            ...(this.isOverwrite ? [{ key: 'columnImport' as const, label: 'Cột lấy dữ liệu', required: true }] : []),
        ];
    }

    override ngOnInit(): void {
        if (this.source() === IMPORT_SOURCE.SHEET) {
            this.loadGoogleSheets();
        } else {
            this.applyHints();
        }
        if (this.isOverwrite) this.loadDocumentFields();
    }

    setValue<K extends keyof IImportColumnMapping>(key: K, value: IImportColumnMapping[K]) {
        this.mapping.update((mapping) => ({ ...mapping, [key]: value }));
    }

    searchHeaders(query: string) {
        const keyword = query.trim().toLocaleLowerCase('vi');
        this.suggestions = this.headers.filter((header) => header.toLocaleLowerCase('vi').includes(keyword));
    }

    // Ghi đè từ Google: đổi link thì đọc lại tên cột của link mới
    onUrlBlur() {
        if (this.mapping().url.trim() !== this._loadedUrl) this.loadGoogleSheets();
    }

    // API sheet-header cần quyền DocumentField.Create; không có quyền thì không gọi
    private loadGoogleSheets() {
        const url = (this.isOverwrite ? this.mapping().url : (this.urlGoogleSheetSync() ?? '')).trim();
        this._loadedUrl = url;
        this._googleSheets = [];
        if (!url || !this._sharedService.isGranted(PermissionConstants.FUNCTION_DOCUMENT_FIELD_CREATE)) {
            this.canReadHeaders = false;
            return;
        }

        this._documentFieldService
            .getSheetHeaders(url)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe({
                next: (res) => {
                    if (url !== this._loadedUrl) return;
                    this.canReadHeaders = this.isResponseSucceed(res, false);
                    if (!this.canReadHeaders) return;
                    this._googleSheets = res.data;
                    this.applyHints();
                },
                error: () => (this.canReadHeaders = false),
            });
    }

    private loadDocumentFields() {
        this._documentFieldService
            .dropdown(this.documentTypeId())
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) this.documentFields = res.data;
            });
    }

    // Điền sẵn ô đang hiện còn trống bằng header trùng tên gợi ý, không phân biệt hoa thường
    private applyHints() {
        const headerByName = new Map(this.headers.map((header) => [header.trim().toLocaleLowerCase('vi'), header]));
        const mapping = { ...this.mapping() };
        for (const { key } of this.columnFields) {
            const hint = IMPORT_COLUMN_HINTS[key];
            if (hint) mapping[key] ||= headerByName.get(hint.toLocaleLowerCase('vi')) ?? '';
        }
        this.mapping.set(mapping);
    }
}
