import { DecimalPipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { TooltipModule } from 'primeng/tooltip';
import { IProjectMyProgress } from '@/models/project.model';

const PROGRESS_TOOLTIPS: Record<string, string> = {
    Data: 'Bạn đã kiểm / văn bản bạn giữ',
    QA: 'Bạn đã chấm / văn bản bạn phụ trách',
};

@Component({
    selector: 'app-project-progress-cell',
    imports: [DecimalPipe, TooltipModule],
    template: `
        <div class="flex flex-col gap-1 min-w-32" [pTooltip]="tooltip()" tooltipPosition="top">
            <div class="h-1.5 rounded-full bg-[var(--app-line)] overflow-hidden">
                <div class="h-full rounded-full bg-[var(--app-accent)]" [style.width.%]="row().progressPercent"></div>
            </div>
            <span class="text-sm text-muted-color">
                {{ row().checkedCount }} / {{ row().totalSubmission }} · {{ row().progressPercent | number: '1.0-2' }}%
            </span>
        </div>
    `,
})
export class ProjectProgressCellComponent {
    row = input.required<Pick<IProjectMyProgress, 'viewerRole' | 'checkedCount' | 'totalSubmission' | 'progressPercent'>>();
    // DataTable CUSTOM_COMP luôn truyền đủ { row, rowIndex, data }
    rowIndex = input<number>();
    data = input<unknown>();

    tooltip = computed(() => PROGRESS_TOOLTIPS[this.row().viewerRole] ?? 'Đã kiểm / tổng văn bản');
}
