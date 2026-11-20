import { Component, computed, input } from '@angular/core';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { SUBMISSION_CHECK_STATUSES, SUBMISSION_QA_STATUS, SUBMISSION_QA_STATUSES } from '@/shared/constants/submission.constants';

type StatusRow = { statusCheck: number; statusQa: number; qaReason: string | null };

const TEMPLATE = `<p-tag [value]="status()?.label ?? '—'" [severity]="status()?.severity" [rounded]="true" [pTooltip]="tooltip()" tooltipPosition="top" />`;

// Ô tag trạng thái Kiểm của bảng văn bản (DataTable CUSTOM_COMP luôn truyền đủ { row, rowIndex, data })
@Component({
    selector: 'app-submission-check-status-cell',
    imports: [TagModule, TooltipModule],
    template: TEMPLATE,
})
export class SubmissionCheckStatusCellComponent {
    row = input.required<StatusRow>();
    rowIndex = input<number>();
    data = input<unknown>();

    status = computed(() => SUBMISSION_CHECK_STATUSES.find((item) => item.value === this.row().statusCheck));
    tooltip = computed(() => '');
}

// Ô tag trạng thái QA; Không đạt thì tooltip lý do QA
@Component({
    selector: 'app-submission-qa-status-cell',
    imports: [TagModule, TooltipModule],
    template: TEMPLATE,
})
export class SubmissionQaStatusCellComponent {
    row = input.required<StatusRow>();
    rowIndex = input<number>();
    data = input<unknown>();

    status = computed(() => SUBMISSION_QA_STATUSES.find((item) => item.value === this.row().statusQa));
    tooltip = computed(() => (this.row().statusQa === SUBMISSION_QA_STATUS.FAILED ? this.row().qaReason ?? '' : ''));
}
