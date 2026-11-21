import { Component, DestroyRef, inject, input, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Utils } from '@sh/ui';
import { finalize, Observable } from 'rxjs';
import { DownloadService } from '@/core/http/download.service';
import { ITransitionAcceptance, IViewProject, IViewProjectAcceptance } from '@/models/project.model';
import { ProjectService } from '@/services/project.service';
import { SharedService } from '@/services/shared.service';
import { BaseComponent } from '@/shared/base/base.component';
import { StatusBadge } from '@/shared/components/status-badge/status-badge.component';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { PROJECT_STATUS, PROJECT_STATUSES } from '@/shared/constants/project.constants';
import { SharedImports } from '@/shared/import.shared';
import { IBaseResponseWithData } from '@/shared/models/request-paging.base.models';
import { ITableBadge } from '@/shared/models/data-table.models';
import { readErrorResponse } from '@/shared/utils/file-response';
import { AcceptanceReasonComponent } from '../../acceptance-reason/acceptance-reason.component';
import { formatNumber, REPORT_COLORS, toPercent } from '../project-report.constants';

const ACCEPTANCE_START_STATUSES: number[] = [PROJECT_STATUS.DOING];
const ACCEPTANCE_REPORT_STATUSES: number[] = [PROJECT_STATUS.ACCEPTING, PROJECT_STATUS.DONE];
// Dải bước; Hết hạn đứng ở bước đầu (tag ở header đã báo Hết hạn)
const ACCEPTANCE_STEPS = [PROJECT_STATUS.DOING, PROJECT_STATUS.ACCEPTING, PROJECT_STATUS.DONE].map(
    (status) => PROJECT_STATUSES.find((item) => item.value === status)!,
);
const STEP_INDEX_BY_STATUS: Record<number, number> = {
    [PROJECT_STATUS.DOING]: 0,
    [PROJECT_STATUS.EXPIRED]: 0,
    [PROJECT_STATUS.ACCEPTING]: 1,
    [PROJECT_STATUS.DONE]: 2,
};
const DAY_MS = 24 * 60 * 60 * 1000;

@Component({
    selector: 'app-report-acceptance',
    imports: [SharedImports, StatusBadge],
    templateUrl: './report-acceptance.component.html',
    styleUrl: './report-acceptance.component.scss',
})
export class ReportAcceptanceComponent extends BaseComponent {
    private readonly _projectService = inject(ProjectService);
    private readonly _downloadService = inject(DownloadService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);

    project = input.required<IViewProject>();
    acceptance = input.required<IViewProjectAcceptance>();
    // Chuyển trạng thái thành công: trang tải lại dự án + nghiệm thu
    changed = output<void>();

    readonly canAccept = this._sharedService.isGranted(PermissionConstants.FUNCTION_PROJECT_ACCEPT);
    readonly steps = ACCEPTANCE_STEPS;
    readonly formatNumber = formatNumber;
    submitting = false;

    get canStart() {
        return this.canAccept && ACCEPTANCE_START_STATUSES.includes(this.project().status);
    }

    get canFinish() {
        return this.canAccept && this.project().status === PROJECT_STATUS.ACCEPTING;
    }

    get canReport() {
        return ACCEPTANCE_REPORT_STATUSES.includes(this.project().status);
    }

    get stepIndex() {
        return STEP_INDEX_BY_STATUS[this.project().status] ?? 0;
    }

    // Đếm như BE (ProjectAcceptanceBlockers): a = chưa kiểm (gồm trả lại), b = đã kiểm nhưng chưa QA đạt
    get blockerMessage() {
        const total = this.acceptance().total;
        if (!total.totalSubmissions) return 'Dự án chưa có văn bản nào';
        const notChecked = total.totalSubmissions - total.totalChecked;
        const notPassed = total.totalChecked - total.totalQaPassed;
        return `Còn ${notChecked} văn bản chưa kiểm` + (this.project().isQaEnabled ? `, ${notPassed} văn bản chưa QA đạt` : '');
    }

    // Chưa đủ điều kiện chỉ Admin được cho phép; dự án chưa có văn bản thì BE chặn cả Admin
    get isTransitionBlocked() {
        return !this.acceptance().isReady && (!this.acceptance().canForce || !this.acceptance().total.totalSubmissions);
    }

    // Thanh tiến độ: QA đạt / tổng, dự án không QA thì đã kiểm / tổng
    get progress() {
        const total = this.acceptance().total;
        const isQaEnabled = this.project().isQaEnabled;
        return {
            label: isQaEnabled ? 'Văn bản đã QA đạt' : 'Văn bản đã kiểm',
            done: isQaEnabled ? total.totalQaPassed : total.totalChecked,
            total: total.totalSubmissions,
            color: isQaEnabled ? REPORT_COLORS.QA_PASSED : REPORT_COLORS.CHECKED,
        };
    }

    get progressPercent() {
        return toPercent(this.progress.done, this.progress.total);
    }

    // Phần còn thiếu tách theo bước (thiết kế 46): chưa kiểm · chờ QA · bị trả; đã đủ thì báo đủ điều kiện
    get pendingMessage() {
        const total = this.acceptance().total;
        const isQaEnabled = this.project().isQaEnabled;
        // Câu luật tự chuyển chỉ đúng khi còn ở bước Đang triển khai
        const rule = this.stepIndex ? '' : isQaEnabled ? 'Tự vào nghiệm thu khi mọi văn bản QA đạt. ' : 'Tự vào nghiệm thu khi mọi văn bản đã kiểm. ';
        if (this.acceptance().isReady) return 'Đủ điều kiện nghiệm thu.';
        if (!total.totalSubmissions) return `${rule}Dự án chưa có văn bản nào.`;
        const parts = [`${formatNumber(total.totalNotChecked)} chưa kiểm`];
        if (isQaEnabled) parts.push(`${formatNumber(total.totalQaNotReviewed)} chờ QA`);
        parts.push(`${formatNumber(total.totalReturned)} bị trả`);
        return `${rule}Còn ${formatNumber(this.progress.total - this.progress.done)}: ${parts.join(' · ')}.`;
    }

    // Còn N ngày tới hạn / Quá hạn N ngày; dự án đã xong hoặc không có hạn thì không hiện
    get deadlineBadge(): ITableBadge | null {
        const endedAt = this.project().endedAt;
        if (!endedAt || this.project().status === PROJECT_STATUS.DONE) return null;
        const today = new Date();
        const end = new Date(endedAt);
        const days = Math.round(
            (new Date(end.getFullYear(), end.getMonth(), end.getDate()).getTime() -
                new Date(today.getFullYear(), today.getMonth(), today.getDate()).getTime()) /
                DAY_MS,
        );
        return days >= 0 ? { label: `Còn ${days} ngày tới hạn`, tone: 'amber' } : { label: `Quá hạn ${-days} ngày`, tone: 'red' };
    }

    startAcceptance() {
        this.confirmTransition('Bắt đầu nghiệm thu', (body) => this._projectService.startAcceptance(this.project().id, body));
    }

    finishAcceptance() {
        this.confirmTransition('Hoàn tất nghiệm thu', (body) => this._projectService.finishAcceptance(this.project().id, body));
    }

    reopenAcceptance() {
        this.openReasonDialog('Trả về đang triển khai', 'Trả dự án về đang triển khai để tiếp tục kiểm / QA.', true, (reason) =>
            this.transition(this._projectService.reopenAcceptance(this.project().id, { reason })),
        );
    }

    downloadReport() {
        const url = this._projectService.acceptanceReportUrl(this.project().id);
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

    private confirmTransition(label: string, request: (body: ITransitionAcceptance) => Observable<IBaseResponseWithData<IViewProjectAcceptance>>) {
        if (this.acceptance().isReady) {
            this.confirmAction({ header: label, message: `${label} dự án ${this.project().name}?` }, () =>
                this.transition(request({ isForced: false })),
            );
            return;
        }
        this.openReasonDialog(label, `${this.blockerMessage}. Vẫn ${label.toLowerCase()} khi chưa đủ điều kiện?`, false, (reason) =>
            this.transition(request({ isForced: true, reason })),
        );
    }

    private openReasonDialog(header: string, message: string, isRequired: boolean, onConfirm: (reason: string) => void) {
        this._dialogService
            .open(AcceptanceReasonComponent, {
                header,
                width: '32rem',
                modal: true,
                closable: true,
                data: { message, isRequired, confirmLabel: header },
            })
            ?.onClose.pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((reason?: string) => reason !== undefined && onConfirm(reason));
    }

    private transition(request$: Observable<IBaseResponseWithData<IViewProjectAcceptance>>) {
        this.submitting = true;
        request$
            .pipe(
                finalize(() => (this.submitting = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res, true, 'Cập nhật nghiệm thu thành công')) {
                    this.changed.emit();
                }
            });
    }
}
