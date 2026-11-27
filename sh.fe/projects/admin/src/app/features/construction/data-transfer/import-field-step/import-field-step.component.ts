import { Component, DestroyRef, inject, input, model, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SkeletonModule } from 'primeng/skeleton';
import { finalize } from 'rxjs';
import { ShSelectComponent } from '@sh/ui';
import { SubmissionService } from '@/services/submission.service';
import { BaseComponent } from '@/shared/base/base.component';
import { StatusBadge } from '@/shared/components/status-badge/status-badge.component';
import { SharedImports } from '@/shared/import.shared';
import { ITableBadge } from '@/shared/models/data-table.models';
import { IBaseResponse } from '@/shared/models/request-paging.base.models';
import { IImportFieldRow, TOAST_ERROR_CODES, toHeaderKey } from '../data-transfer.constants';

// Bước 2 wizard import: ghép từng trường metadata của cặp vào một cột trên sheet, lưu cho dự án (63.4, 63.5)
@Component({
    selector: 'app-import-field-step',
    imports: [SharedImports, ShSelectComponent, SkeletonModule, StatusBadge],
    templateUrl: './import-field-step.component.html',
})
export class ImportFieldStepComponent extends BaseComponent {
    private readonly _submissionService = inject(SubmissionService);
    private readonly _destroyRef = inject(DestroyRef);

    projectId = input.required<number>();
    documentTypeId = input.required<number>();
    // Header đầy đủ của sheet đã chọn ở bước 1; columnIndex là vị trí trong mảng này
    headers = input.required<string[]>();
    startColumn = input.required<number>();
    // Lỗi lưu ghép từ wizard (BE 309 / 311 / 400 ...)
    saveError = input<string | null>(null);
    // null = chưa tải ghép từ BE; wizard giữ để quay lại bước này không gọi lại match (63.10)
    rows = model.required<IImportFieldRow[] | null>();
    // Lỗi tải ghép báo bằng toast (211 / 337, 63.13): wizard xử lý, bước này không hiện khung đỏ
    toastFailed = output<IBaseResponse>();

    readonly mappedBadge: ITableBadge = { label: 'Đã ghép', tone: 'green' };
    readonly unmappedBadge: ITableBadge = { label: 'Chưa chọn cột', tone: 'red' };

    isLoading = false;
    loadError: string | null = null;
    // Bấm "Tiếp tục" khi còn trường chưa chọn cột thì bật cờ này để viền đỏ các dòng đó
    showRequired = false;

    // Cột chọn được: chỉ từ "Bắt đầu đọc từ cột" trở đi, bỏ header rỗng
    private get selectableColumns() {
        return this.headers()
            .map((header, index) => ({ header, index }))
            .filter((column) => column.index >= this.startColumn() - 1 && column.header.trim() !== '');
    }

    // Header trùng tên hiện kèm chữ cột "Ghi chú (cột thứ 6)" để phân biệt (63.4)
    get columnOptions() {
        const countByKey = this.countByHeaderKey(this.selectableColumns.map((column) => column.header));
        return this.selectableColumns.map((column) => ({
            value: column.index,
            label: countByKey.get(toHeaderKey(column.header))! > 1 ? `${column.header.trim()} (cột thứ ${column.index + 1})` : column.header.trim(),
        }));
    }

    get mappedCount() {
        return (this.rows() ?? []).filter((row) => row.columnIndex !== null).length;
    }

    // Trường nào đang dùng chung một cột với trường khác
    get conflictFieldIds() {
        const rows = (this.rows() ?? []).filter((row) => row.columnIndex !== null);
        return new Set(rows.filter((row) => rows.some((other) => other !== row && other.columnIndex === row.columnIndex)).map((row) => row.documentFieldId));
    }

    // Tên cột xuất hiện k lần trên sheet mà ghép ít hơn k trường (BE 309), chỉ xét tên đã có trường chọn
    get repeatedHeaderMessages() {
        const countByKey = this.countByHeaderKey(this.selectableColumns.map((column) => column.header));
        const chosenHeaders = (this.rows() ?? []).filter((row) => row.columnIndex !== null).map((row) => this.headers()[row.columnIndex!]);
        const chosenCountByKey = this.countByHeaderKey(chosenHeaders);
        return [...countByKey]
            .filter(([key, count]) => count > 1 && (chosenCountByKey.get(key) ?? 0) > 0 && chosenCountByKey.get(key)! < count)
            .map(([key, count]) => {
                const header = this.selectableColumns.find((column) => toHeaderKey(column.header) === key)!.header.trim();
                return `Cột ${header} xuất hiện ${count} lần trên sheet, phải ghép đủ ${count} trường hoặc đổi tên cột trên sheet`;
            });
    }

    // Header chưa ghép vào trường nào (hiện chip xám, như soHoaSaoMai)
    get unusedHeaders() {
        const usedIndexes = new Set((this.rows() ?? []).map((row) => row.columnIndex));
        return this.selectableColumns.filter((column) => !usedIndexes.has(column.index)).map((column) => column.header.trim());
    }

    // Khoá "Tiếp tục": đang tải / lỗi tải / không có trường / trùng cột / tên cột lặp chưa ghép đủ. Thiếu cột thì không khoá, báo đỏ khi bấm (63.5)
    get isBlocked() {
        const rows = this.rows();
        return !rows?.length || this.conflictFieldIds.size > 0 || this.repeatedHeaderMessages.length > 0;
    }

    override ngOnInit(): void {
        if (this.rows() === null) this.load();
    }

    setColumn(row: IImportFieldRow, columnIndex: number | null) {
        this.rows.update((rows) => rows!.map((item) => (item === row ? { ...item, columnIndex } : item)));
    }

    // Còn trường chưa chọn cột thì viền đỏ, cuộn tới dòng đỏ đầu và không cho sang bước (63.5)
    validateRequired() {
        const firstMissing = this.rows()!.find((row) => row.columnIndex === null);
        this.showRequired = true;
        if (firstMissing) setTimeout(() => document.getElementById(`importField${firstMissing.documentFieldId}`)?.scrollIntoView({ behavior: 'smooth', block: 'center' }));
        return !firstMissing;
    }

    private load() {
        this.isLoading = true;
        this.loadError = null;
        this._submissionService
            .matchImportFieldMapping({
                projectId: this.projectId(),
                documentTypeId: this.documentTypeId(),
                headers: this.headers(),
                startColumn: this.startColumn(),
            })
            .pipe(
                finalize(() => (this.isLoading = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res, false)) {
                    if (TOAST_ERROR_CODES.includes(res.code)) {
                        this.toastFailed.emit(res);
                    } else {
                        this.loadError = res.message;
                    }
                    return;
                }
                this.rows.set(res.data.map((field) => ({ documentFieldId: field.documentFieldId, name: field.name, columnIndex: field.matchedColumnIndex })));
            });
    }

    private countByHeaderKey(headers: string[]) {
        return headers.reduce((counts, header) => counts.set(toHeaderKey(header), (counts.get(toHeaderKey(header)) ?? 0) + 1), new Map<string, number>());
    }
}
