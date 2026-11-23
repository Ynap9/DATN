import { Component, DestroyRef, effect, inject, input, output, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { CheckboxModule } from 'primeng/checkbox';
import { debounceTime, finalize, Subscription, switchMap, takeWhile, timer } from 'rxjs';
import { ShSelectComponent } from '@sh/ui';
import { IViewProjectPair } from '@/models/project.model';
import { ISubmissionExportJob, ISubmissionFilter } from '@/models/submission.model';
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
import { EXPORT_DEFAULT_COLUMNS, formatNumber, IExportJobRow } from '../data-transfer.constants';

const SEARCH_DEBOUNCE_MS = 400;

// Tab Xuất Excel: lọc văn bản + chọn cột thêm, tạo job xuất chạy nền và theo dõi tới khi tải được (48.16, 48.17)
@Component({
    selector: 'app-export-panel',
    imports: [SharedImports, CheckboxModule, ShSelectComponent],
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
    readonly formatNumber = formatNumber;
    readonly keyword = new FormControl('', { nonNullable: true });

    statusCheck: number | null = null;
    statusQa: number | null = null;
    selectedColumns = new Set(EXPORT_DEFAULT_COLUMNS);
    // null = đang đếm
    total: number | null = null;
    isCreating = false;
    jobs: IExportJobRow[] = [];
    private _countSubscription: Subscription | null = null;

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

    constructor() {
        super();
        effect(() => {
            const pair = this.pair();
            untracked(() => {
                if (!pair.isQaEnabled) this.statusQa = null;
                this.loadCount();
            });
        });
        this.keyword.valueChanges.pipe(debounceTime(SEARCH_DEBOUNCE_MS), takeUntilDestroyed(this._destroyRef)).subscribe(() => this.loadCount());
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
                const row: IExportJobRow = { ...res.data, downloadUrl: null, errorMessage: null, readyAt: null };
                this.jobs = [row, ...this.jobs];
                this.pollJob(row);
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

    // Lỗi đã toast câu BE trả; 337 thì báo màn cha
    private isSucceed(res: IBaseResponse) {
        if (this.isResponseSucceed(res)) return true;
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
