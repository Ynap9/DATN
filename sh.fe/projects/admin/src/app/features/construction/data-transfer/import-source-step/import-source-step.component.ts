import { HttpEventType } from '@angular/common/http';
import { Component, DestroyRef, effect, inject, input, model, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, Subscription } from 'rxjs';
import { ShInputNumberComponent, ShSelectComponent, Utils } from '@sh/ui';
import { DownloadService } from '@/core/http/download.service';
import { ISheetHeader } from '@/models/document-field.model';
import { IUploadImportExcelResult } from '@/models/submission.model';
import { DocumentTypeService } from '@/services/document-type.service';
import { SharedService } from '@/services/shared.service';
import { SubmissionService } from '@/services/submission.service';
import { BaseComponent } from '@/shared/base/base.component';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { IMPORT_EXCEL_MAX_BYTES, IMPORT_SOURCE, ImportSource } from '@/shared/constants/submission.constants';
import { SharedImports } from '@/shared/import.shared';
import { readErrorResponse } from '@/shared/utils/file-response';
import { IMPORT_DEFAULT_START_COLUMN, IMPORT_SOURCE_OPTIONS } from '../data-transfer.constants';

const EXCEL_EXTENSION = '.xlsx';

// Bước 1 wizard import: chọn nguồn Google Sheet (dán link bất kỳ, lấy danh sách sheet) / Excel (tải file, chọn sheet) và cột bắt đầu đọc (63.2, 63.3)
@Component({
    selector: 'app-import-source-step',
    imports: [SharedImports, ShSelectComponent, ShInputNumberComponent],
    templateUrl: './import-source-step.component.html',
})
export class ImportSourceStepComponent extends BaseComponent {
    private readonly _submissionService = inject(SubmissionService);
    private readonly _documentTypeService = inject(DocumentTypeService);
    private readonly _downloadService = inject(DownloadService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);

    projectId = input.required<number>();
    documentTypeId = input.required<number>();
    // Link và tên sheet đang gắn với dự án: điền sẵn + chọn sẵn sheet; googleSheetName null = chưa tải xong
    urlGoogleSheetSync = input<string | null>(null);
    googleSheetName = input<string | null>(null);
    source = model.required<ImportSource>();
    upload = model<IUploadImportExcelResult | null>(null);
    sheetName = model<string | null>(null);
    url = model.required<string>();
    googleSheets = model<ISheetHeader[]>([]);
    googleSheet = model<string | null>(null);
    startColumn = model.required<number>();

    readonly sourceKey = IMPORT_SOURCE;
    readonly sourceOptions = IMPORT_SOURCE_OPTIONS;

    fileError: string | null = null;
    // Lỗi đọc link hiện dưới ô link, không toast (63.3)
    sheetsError: string | null = null;
    isLoadingSheets = false;
    // null = không tải; 0–100 = % đang tải lên
    uploadPercent: number | null = null;
    isDragging = false;
    private _uploadSubscription: Subscription | null = null;
    private _sheetsSubscription: Subscription | null = null;

    get sheetOptions() {
        return this.toSheetOptions(this.upload()?.sheets ?? []);
    }

    get googleSheetOptions() {
        return this.toSheetOptions(this.googleSheets());
    }

    // Số cột của sheet đang chọn là trần của "Bắt đầu đọc từ cột"
    get maxStartColumn() {
        const isGoogle = this.source() === IMPORT_SOURCE.SHEET;
        const sheets = isGoogle ? this.googleSheets() : (this.upload()?.sheets ?? []);
        const sheetName = isGoogle ? this.googleSheet() : this.sheetName();
        return sheets.find((sheet) => sheet.sheetName === sheetName)?.headers.length ?? IMPORT_DEFAULT_START_COLUMN;
    }

    get hasSelectedSheet() {
        return this.source() === IMPORT_SOURCE.SHEET ? !!this.googleSheet() : !!this.upload() && !!this.sheetName();
    }

    // Tải mẫu cần DocumentType.View; thiếu quyền thì ẩn link
    get canDownloadTemplate() {
        return this._sharedService.isGranted(PermissionConstants.FUNCTION_DOCUMENT_TYPE_VIEW);
    }

    constructor() {
        super();
        // Link của dự án điền sẵn thì tự lấy danh sách sheet một lần; chờ có tên sheet của dự án để chọn sẵn đúng sheet
        effect(() => {
            const isProjectUrl = this.url().trim() !== '' && this.url().trim() === this.urlGoogleSheetSync();
            const shouldAutoLoad = this.source() === IMPORT_SOURCE.SHEET && isProjectUrl && !this.googleSheets().length && this.googleSheetName() !== null;
            if (shouldAutoLoad) untracked(() => this.loadSheets());
        });
    }

    selectSource(source: ImportSource) {
        this.source.set(source);
    }

    // Sửa link thì danh sách sheet, sheet đã chọn và lỗi cũ không còn đúng; huỷ lượt gọi dở (63.3)
    onUrlChange(url: string) {
        this._sheetsSubscription?.unsubscribe();
        this.isLoadingSheets = false;
        this.url.set(url);
        this.googleSheets.set([]);
        this.googleSheet.set(null);
        this.sheetsError = null;
        this.startColumn.set(IMPORT_DEFAULT_START_COLUMN);
    }

    // Lỗi hiện dưới ô link; thành công chọn sẵn sheet của dự án (khi link là link dự án và có sheet đó), không thì sheet đầu
    loadSheets() {
        this._sheetsSubscription?.unsubscribe();
        this.isLoadingSheets = true;
        const url = this.url().trim();
        this._sheetsSubscription = this._submissionService
            .getImportSheetHeaders({ projectId: this.projectId(), documentTypeId: this.documentTypeId(), url })
            .pipe(
                finalize(() => (this.isLoadingSheets = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                const isSucceed = this.isResponseSucceed(res, false);
                const sheets = isSucceed ? res.data : [];
                const projectSheet = url === this.urlGoogleSheetSync() ? sheets.find((sheet) => sheet.sheetName === this.googleSheetName()) : undefined;
                this.googleSheets.set(sheets);
                this.sheetsError = isSucceed ? null : res.message;
                this.onSheetChange((projectSheet ?? sheets[0])?.sheetName ?? null);
            });
    }

    // Đổi sheet (Google hoặc Excel): cột bắt đầu không được vượt số cột của sheet mới
    onSheetChange(sheetName: string | null) {
        if (this.source() === IMPORT_SOURCE.SHEET) {
            this.googleSheet.set(sheetName);
        } else {
            this.sheetName.set(sheetName);
        }
        this.startColumn.update((startColumn) => Math.min(startColumn, this.maxStartColumn));
    }

    onStartColumnChange(startColumn: number | null) {
        this.startColumn.set(startColumn ?? IMPORT_DEFAULT_START_COLUMN);
    }

    downloadTemplate() {
        const url = this._documentTypeService.excelTemplateUrl(this.documentTypeId());
        this._downloadService
            .get(url)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe(async (response) => {
                const error = await readErrorResponse(response);
                if (error) {
                    this.isResponseSucceed(error);
                    return;
                }
                Utils.makeDownload(response, url);
            });
    }

    onFileSelected(event: Event) {
        const fileInput = event.target as HTMLInputElement;
        const file = fileInput.files?.[0];
        // Cho phép chọn lại đúng file vừa chọn
        fileInput.value = '';
        if (file) this.uploadFile(file);
    }

    onDrop(event: DragEvent) {
        event.preventDefault();
        this.isDragging = false;
        const file = event.dataTransfer?.files[0];
        if (file) this.uploadFile(file);
    }

    onDragOver(event: DragEvent) {
        event.preventDefault();
        this.isDragging = true;
    }

    // Bỏ file đã tải để chọn file khác; huỷ lượt tải đang chạy nếu có
    changeFile() {
        this._uploadSubscription?.unsubscribe();
        this.uploadPercent = null;
        this.fileError = null;
        this.upload.set(null);
        this.sheetName.set(null);
        this.startColumn.set(IMPORT_DEFAULT_START_COLUMN);
    }

    // FE chặn đuôi khác .xlsx / quá 20 MB, hợp lệ mới gửi; xong chọn sẵn sheet đầu tiên
    private uploadFile(file: File) {
        if (!file.name.toLowerCase().endsWith(EXCEL_EXTENSION) || file.size > IMPORT_EXCEL_MAX_BYTES) {
            this.fileError = 'Chỉ nhận .xlsx tối đa 20 MB';
            return;
        }

        this.changeFile();
        this.uploadPercent = 0;
        this._uploadSubscription = this._submissionService
            .uploadImportExcel(file)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe({
                next: (event) => {
                    if (event.type === HttpEventType.UploadProgress && event.total) {
                        this.uploadPercent = Math.round((event.loaded / event.total) * 100);
                    }
                    if (event.type !== HttpEventType.Response) return;

                    this.uploadPercent = null;
                    const res = event.body;
                    if (!res || !this.isResponseSucceed(res)) return;
                    this.upload.set(res.data);
                    this.onSheetChange(res.data.sheets[0]?.sheetName ?? null);
                },
                error: () => (this.uploadPercent = null),
            });
    }

    private toSheetOptions(sheets: ISheetHeader[]) {
        return sheets.map((sheet) => ({ value: sheet.sheetName, label: `${sheet.sheetName} (${sheet.headers.length} cột)` }));
    }
}
