import { Component, computed, DestroyRef, effect, inject, input, output, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { CheckboxModule } from 'primeng/checkbox';
import { debounceTime, finalize, Subscription, switchMap, takeWhile, timer } from 'rxjs';
import { ShInputNumberComponent, ShSelectComponent } from '@sh/ui';
import { IViewProjectPair } from '@/models/project.model';
import { ISubmissionExportJob, ISubmissionExportSheetJob, ISubmissionFilter } from '@/models/submission.model';
import { SubmissionService } from '@/services/submission.service';
import { BaseComponent } from '@/shared/base/base.component';
import {
    BACKGROUND_JOB_STATUS,
    EXPORT_COLUMN_GROUP,
    EXPORT_COLUMNS,
    EXPORT_DOWNLOAD_URL_MS,
    JOB_POLL_MS,
    SUBMISSION_CHECK_STATUSES,
    SUBMISSION_ERROR_CODE,
    SUBMISSION_QA_STATUSES,
} from '@/shared/constants/submission.constants';
import { SharedImports } from '@/shared/import.shared';
import { IBaseResponse } from '@/shared/models/request-paging.base.models';
import { EXPORT_DEFAULT_COLUMNS, EXPORT_TARGET, EXPORT_TARGET_OPTIONS, ExportTarget, formatNumber, IExportJobRow } from '../data-transfer.constants';

const SEARCH_DEBOUNCE_MS = 400;

// Tab Xuất dữ liệu: chọn đích (Excel / Google Sheet), lọc văn bản + chọn cột thêm, tạo job xuất chạy nền và theo dõi tới khi xong (48.16, 48.17, 56)
@Component({
    selector: 'app-export-panel',
    imports: [SharedImports, CheckboxModule, ShSelectComponent, ShInputNumberComponent],
    templateUrl: './export-panel.component.html',
})
export class ExportPanelComponent extends BaseComponent {
    private readonly _submissionService = inject(SubmissionService);
    private readonly _destroyRef = inject(DestroyRef);

    pair = input.required<IViewProjectPair>();
    // 337: không còn quản lý dự án, màn cha tải lại danh sách cặp (48.18)
    forbidden = output<void>();

    readonly jobStatus = BACKGROUND_JOB_STATUS;
    readonly checkStatusOptions = SUBMISSION_CHECK_STATUSES;
    readonly qaStatusOptions = SUBMISSION_QA_STATUSES;
    readonly targetOptions = EXPORT_TARGET_OPTIONS;
    readonly targetKey = EXPORT_TARGET;
    readonly formatNumber = formatNumber;
    readonly keyword = new FormControl('', { nonNullable: true });

    target: ExportTarget = EXPORT_TARGET.EXCEL;
    // Đích Google Sheet (56.3): link do người dùng dán, tên sheet chọn từ danh sách BE trả
    sheetUrl = '';
    sheetNames: string[] = [];
    sheetName: string | null = null;
    startRow: number | null = 1;
    sheetNamesError: string | null = null;
    isLoadingSheetNames = false;
    statusCheck: number | null = null;
    statusQa: number | null = null;
    selectedColumns = new Set(EXPORT_DEFAULT_COLUMNS);
    // null = đang đếm
    total: number | null = null;
    isCreating = false;
    jobs: IExportJobRow[] = [];
    private _countSubscription: Subscription | null = null;
    private _sheetNamesSubscription: Subscription | null = null;
    // Đổi cặp dự án / loại (tải lại cùng cặp thì không đổi) mới làm lại khối đích Sheet (56.6)
    private readonly _pairKey = computed(() => `${this.pair().projectId}-${this.pair().documentTypeId}`);

    // Nhóm hộp tick theo thứ tự EXPORT_COLUMN_GROUP; dự án không QA thì ẩn cả nhóm QA
    get columnGroups() {
        return Object.values(EXPORT_COLUMN_GROUP)
            .filter((group) => group !== EXPORT_COLUMN_GROUP.QA || this.pair().isQaEnabled)
            .map((group) => ({ name: group, columns: EXPORT_COLUMNS.filter((column) => column.group === group) }));
    }

    // Cột thêm gửi lên: chỉ cột đang hiện và đang tick
    get activeColumns() {
        return this.columnGroups.flatMap((group) => group.columns.map((column) => column.value)).filter((value) => this.selectedColumns.has(value));
    }

    get isAllSelected() {
        return this.columnGroups.every((group) => group.columns.every((column) => this.selectedColumns.has(column.value)));
    }

    // Khoá nút Xuất Google Sheet (56.8); còn dòng "Đang ghi…" cùng link + tên sheet thì khoá để không có hai job cùng ghi một tab
    get isSheetExportDisabled() {
        const url = this.sheetUrl.trim();
        const isWriting = this.jobs.some(
            (job) => job.target === EXPORT_TARGET.SHEET && job.status < BACKGROUND_JOB_STATUS.DONE && job.sheetUrl === url && job.sheetName === this.sheetName,
        );
        return !this.total || this.isCreating || !url || !this.sheetName || !this.startRow || this.startRow < 1 || isWriting;
    }

    constructor() {
        super();
        effect(() => {
            this._pairKey();
            untracked(() => this.resetSheetTarget());
        });
        effect(() => {
            const pair = this.pair();
            untracked(() => {
                if (!pair.isQaEnabled) this.statusQa = null;
                this.loadCount();
            });
        });
        this.keyword.valueChanges.pipe(debounceTime(SEARCH_DEBOUNCE_MS), takeUntilDestroyed(this._destroyRef)).subscribe(() => this.loadCount());
    }

    selectTarget(target: ExportTarget) {
        this.target = target;
    }

    // Sửa link thì danh sách sheet, tên sheet và câu lỗi cũ không còn đúng, phải bấm lấy lại (56.5)
    onSheetUrlChange(url: string) {
        this.sheetUrl = url;
        this.clearSheetNames();
    }

    // Lỗi hiện dưới ô link thay vì toast (56.4); thành công thì chọn sẵn sheet đầu
    loadSheetNames() {
        this._sheetNamesSubscription?.unsubscribe();
        this.isLoadingSheetNames = true;
        this._sheetNamesSubscription = this._submissionService
            .getExportSheetNames({ projectId: this.pair().projectId, documentTypeId: this.pair().documentTypeId, url: this.sheetUrl.trim() })
            .pipe(
                finalize(() => (this.isLoadingSheetNames = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                const isSucceed = this.isSucceed(res, false);
                this.sheetNames = isSucceed ? res.data : [];
                this.sheetName = this.sheetNames[0] ?? null;
                this.sheetNamesError = isSucceed ? null : res.message;
            });
    }

    // Hỏi xác nhận vì job xoá mọi ô từ dòng bắt đầu trở xuống (56.9)
    exportSheet() {
        const { total, sheetName, startRow } = this;
        if (!total || !sheetName || !startRow) return;
        this._confirmationService.confirm({
            header: 'Xuất ra Google Sheet?',
            message: `Ghi ${formatNumber(total)} văn bản vào sheet ${sheetName}, bắt đầu từ dòng ${startRow}. Mọi dữ liệu từ dòng ${startRow} trở xuống của sheet này sẽ bị xoá rồi ghi lại. Không hoàn tác được.`,
            icon: 'pi pi-exclamation-triangle',
            closable: true,
            closeOnEscape: true,
            rejectButtonProps: { label: 'Huỷ', severity: 'secondary', outlined: true },
            acceptButtonProps: { label: 'Xuất' },
            accept: () => this.createSheetJob(total, sheetName, startRow),
        });
    }

    onStatusCheckChange(value: number | null) {
        this.statusCheck = value;
        this.loadCount();
    }

    onStatusQaChange(value: number | null) {
        this.statusQa = value;
        this.loadCount();
    }

    toggleColumn(value: string, checked: boolean) {
        if (checked) {
            this.selectedColumns.add(value);
        } else {
            this.selectedColumns.delete(value);
        }
    }

    toggleAll() {
        const values = this.columnGroups.flatMap((group) => group.columns.map((column) => column.value));
        this.selectedColumns = this.isAllSelected ? new Set() : new Set([...this.selectedColumns, ...values]);
    }

    exportExcel() {
        if (!this.total || this.isCreating) return;
        this.isCreating = true;
        this._submissionService
            .createExportJob({ ...this.buildFilter(), columns: this.activeColumns })
            .pipe(
                finalize(() => (this.isCreating = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isSucceed(res)) return;
                const row: IExportJobRow = { ...res.data, target: EXPORT_TARGET.EXCEL, downloadUrl: null, errorMessage: null, readyAt: null };
                this.jobs = [row, ...this.jobs];
                this.pollJob(row);
            });
    }

    // Dòng Sheet ghi lại link và tên sheet đã dùng, số văn bản lúc bấm để hiện trong danh sách "Đã xuất" (56.10, 56.11)
    private createSheetJob(total: number, sheetName: string, startRow: number) {
        const url = this.sheetUrl.trim();
        this.isCreating = true;
        this._submissionService
            .createExportSheetJob({ ...this.buildFilter(), columns: this.activeColumns, url, sheetName, startRow })
            .pipe(
                finalize(() => (this.isCreating = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isSucceed(res)) return;
                const row: IExportJobRow = {
                    jobId: res.data.jobId,
                    target: EXPORT_TARGET.SHEET,
                    fileName: '',
                    sheetUrl: url,
                    sheetName,
                    startRow,
                    total,
                    status: res.data.status,
                    downloadUrl: null,
                    errorMessage: null,
                    readyAt: null,
                };
                this.jobs = [row, ...this.jobs];
                this.pollSheetJob(row);
            });
    }

    // Link quá 15 phút thì hỏi lại job để lấy link mới rồi mới mở
    download(row: IExportJobRow) {
        if (row.downloadUrl && row.readyAt && Date.now() - row.readyAt < EXPORT_DOWNLOAD_URL_MS) {
            window.open(row.downloadUrl, '_blank');
            return;
        }

        this._submissionService
            .findExportJob(row.jobId)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                this.applyJob(row, res.data, res.message, this.isResponseSucceed(res));
                if (row.downloadUrl) window.open(row.downloadUrl, '_blank');
            });
    }

    // Hỏi job mỗi JOB_POLL_MS tới khi xong / lỗi; 326 (job không thấy) cũng dừng
    private pollJob(row: IExportJobRow) {
        timer(0, JOB_POLL_MS)
            .pipe(
                switchMap(() => this._submissionService.findExportJob(row.jobId)),
                takeWhile((res) => this.isResponseSucceed(res, false) && res.data.status < BACKGROUND_JOB_STATUS.DONE, true),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => this.applyJob(row, res.data, res.message, this.isResponseSucceed(res, false)));
    }

    // Lỗi API (326…) coi như job lỗi, hiện câu BE trả dưới tên file
    private applyJob(row: IExportJobRow, job: ISubmissionExportJob, message: string, isSucceed: boolean) {
        if (!isSucceed) {
            row.status = BACKGROUND_JOB_STATUS.FAILED;
            row.errorMessage = message;
            return;
        }
        row.status = job.status;
        row.errorMessage = job.errorMessage;
        row.downloadUrl = job.downloadUrl;
        row.readyAt = job.downloadUrl ? Date.now() : null;
    }

    // Như pollJob cho job ghi Google Sheet (56.12)
    private pollSheetJob(row: IExportJobRow) {
        timer(0, JOB_POLL_MS)
            .pipe(
                switchMap(() => this._submissionService.findExportSheetJob(row.jobId)),
                takeWhile((res) => this.isResponseSucceed(res, false) && res.data.status < BACKGROUND_JOB_STATUS.DONE, true),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => this.applySheetJob(row, res.data, res.message, this.isResponseSucceed(res, false)));
    }

    // Lỗi API (326…) coi như dòng lỗi, hiện câu BE trả dưới dòng
    private applySheetJob(row: IExportJobRow, job: ISubmissionExportSheetJob, message: string, isSucceed: boolean) {
        if (!isSucceed) {
            row.status = BACKGROUND_JOB_STATUS.FAILED;
            row.errorMessage = message;
            return;
        }
        row.status = job.status;
        row.errorMessage = job.errorMessage;
    }

    private clearSheetNames() {
        this._sheetNamesSubscription?.unsubscribe();
        this.sheetNames = [];
        this.sheetName = null;
        this.sheetNamesError = null;
    }

    // Link để trống cho người dùng dán (không điền sẵn Sheet của dự án, vì đó là nguồn import), dòng bắt đầu về 1
    private resetSheetTarget() {
        this.clearSheetNames();
        this.sheetUrl = '';
        this.startRow = 1;
    }

    // Số "Sẽ xuất" theo đúng bộ lọc; đổi lọc nhanh thì bỏ lượt đếm cũ
    private loadCount() {
        this._countSubscription?.unsubscribe();
        this.total = null;
        this._countSubscription = this._submissionService
            .count(this.buildFilter())
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (this.isSucceed(res)) this.total = res.data.total;
            });
    }

    // Lỗi đã toast câu BE trả (trừ khi isShowErrorMsg = false); 337 thì báo màn cha
    private isSucceed(res: IBaseResponse, isShowErrorMsg = true) {
        if (this.isResponseSucceed(res, isShowErrorMsg)) return true;
        if (res.code === SUBMISSION_ERROR_CODE.PROJECT_NOT_MANAGED) this.forbidden.emit();
        return false;
    }

    private buildFilter(): ISubmissionFilter {
        return {
            projectId: this.pair().projectId,
            documentTypeId: this.pair().documentTypeId,
            statusCheck: this.statusCheck ?? undefined,
            statusQa: this.statusQa ?? undefined,
            keyword: this.keyword.value.trim() || undefined,
        };
    }
}
