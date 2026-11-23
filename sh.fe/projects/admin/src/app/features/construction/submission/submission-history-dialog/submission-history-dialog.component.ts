import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DialogService, DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { finalize } from 'rxjs';
import { ISubmissionTimeline } from '@/models/submission.model';
import { SubmissionService } from '@/services/submission.service';
import { BaseComponent } from '@/shared/base/base.component';
import { SUBMISSION_CHECK_STATUS, SUBMISSION_QA_STATUS, TIMELINE_EVENT } from '@/shared/constants/submission.constants';
import { SharedImports } from '@/shared/import.shared';

export interface ISubmissionHistoryDialogData {
    submissionId: number;
}

// Tiêu đề lượt kiểm theo trạng thái trước khi kiểm
const CHECK_TITLES: Record<number, string> = {
    [SUBMISSION_CHECK_STATUS.NOT_CHECKED]: 'Kiểm lần đầu',
    [SUBMISSION_CHECK_STATUS.CHECKED]: 'Tái kiểm',
    [SUBMISSION_CHECK_STATUS.RETURNED]: 'Kiểm lại sau khi bị trả',
};

interface IHistoryItem {
    event: ISubmissionTimeline;
    isCheck: boolean;
    isQaFailed: boolean;
    title: string;
    dotClass: string;
    userLabel: string;
}

// Mở dialog Lịch sử văn bản — dùng chung cho màn danh sách và màn Nhập liệu (43.16)
export function openSubmissionHistory(dialogService: DialogService, submissionId: number, fileName: string) {
    const data: ISubmissionHistoryDialogData = { submissionId };
    return dialogService.open(SubmissionHistoryDialogComponent, {
        header: `Lịch sử — ${fileName}`,
        width: '720px',
        breakpoints: { '768px': '95vw' },
        contentStyle: { 'max-height': '70vh', overflow: 'auto' },
        modal: true,
        closable: true,
        data,
    });
}

// Các lượt kiểm / QA của một văn bản, mới nhất trên cùng
@Component({
    selector: 'app-submission-history-dialog',
    imports: [SharedImports],
    templateUrl: './submission-history-dialog.component.html',
})
export class SubmissionHistoryDialogComponent extends BaseComponent implements OnInit {
    private readonly _submissionService = inject(SubmissionService);
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _data: ISubmissionHistoryDialogData = inject(DynamicDialogConfig).data;

    items: IHistoryItem[] = [];
    loaded = false;

    override ngOnInit(): void {
        this.loading = true;
        this._submissionService
            .timeline(this._data.submissionId)
            .pipe(
                finalize(() => (this.loading = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                // Lỗi (820 / 305 …): toast câu BE trả rồi đóng dialog
                if (!this.isResponseSucceed(res)) {
                    this._ref.close();
                    return;
                }
                this.items = [...res.data].reverse().map((event) => toHistoryItem(event));
                this.loaded = true;
            });
    }
}

// BE trả cũ → mới; CHECK chấm xanh dương, QA chấm xanh lá (Đạt) / đỏ (Không đạt)
function toHistoryItem(event: ISubmissionTimeline): IHistoryItem {
    const isCheck = event.eventType === TIMELINE_EVENT.CHECK;
    const isQaPassed = event.newStatusQa === SUBMISSION_QA_STATUS.PASSED;
    return {
        event,
        isCheck,
        isQaFailed: !isCheck && event.newStatusQa === SUBMISSION_QA_STATUS.FAILED,
        title: isCheck ? CHECK_TITLES[event.oldStatusCheck ?? SUBMISSION_CHECK_STATUS.NOT_CHECKED] : isQaPassed ? 'QA Đạt' : 'QA Không đạt',
        dotClass: isCheck ? 'bg-blue-500' : isQaPassed ? 'bg-green-500' : 'bg-red-500',
        userLabel: event.fullName ? `${event.fullName} (${event.userName ?? ''})` : (event.userName ?? ''),
    };
}
