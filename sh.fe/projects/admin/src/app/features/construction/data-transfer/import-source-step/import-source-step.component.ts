import { HttpEventType } from '@angular/common/http';
import { Component, DestroyRef, inject, input, model } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription } from 'rxjs';
import { ShSelectComponent } from '@sh/ui';
import { IUploadImportExcelResult } from '@/models/submission.model';
import { SubmissionService } from '@/services/submission.service';
import { BaseComponent } from '@/shared/base/base.component';
import { IMPORT_EXCEL_MAX_BYTES, IMPORT_SOURCE, ImportSource } from '@/shared/constants/submission.constants';
import { SharedImports } from '@/shared/import.shared';
import { IMPORT_SOURCE_OPTIONS } from '../data-transfer.constants';

const EXCEL_EXTENSION = '.xlsx';

// Bước 1 wizard import: chọn nguồn Google Sheet / Excel, tải file Excel lên và chọn sheet (48.7)
@Component({
    selector: 'app-import-source-step',
    imports: [SharedImports, ShSelectComponent],
    templateUrl: './import-source-step.component.html',
})
export class ImportSourceStepComponent extends BaseComponent {
    private readonly _submissionService = inject(SubmissionService);
    private readonly _destroyRef = inject(DestroyRef);

    urlGoogleSheetSync = input<string | null>(null);
    googleSheetName = input<string | null>(null);
    source = model.required<ImportSource>();
    upload = model<IUploadImportExcelResult | null>(null);
    sheetName = model<string | null>(null);

    readonly sourceKey = IMPORT_SOURCE;
    readonly sourceOptions = IMPORT_SOURCE_OPTIONS;

    fileError: string | null = null;
    // null = không tải; 0–100 = % đang tải lên
    uploadPercent: number | null = null;
    isDragging = false;
    private _uploadSubscription: Subscription | null = null;

    get sheetOptions() {
        return (this.upload()?.sheets ?? []).map((sheet) => ({ value: sheet.sheetName, label: `${sheet.sheetName} (${sheet.headers.length} cột)` }));
    }

    isSourceDisabled(source: ImportSource) {
        return source === IMPORT_SOURCE.SHEET && !this.urlGoogleSheetSync();
    }

    selectSource(source: ImportSource) {
        if (this.isSourceDisabled(source)) return;
        this.source.set(source);
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
                    this.sheetName.set(res.data.sheets[0]?.sheetName ?? null);
                },
                error: () => (this.uploadPercent = null),
            });
    }
}
