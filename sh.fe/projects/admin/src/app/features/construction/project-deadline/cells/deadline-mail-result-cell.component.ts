import { Component, input } from '@angular/core';
import { PopoverModule } from 'primeng/popover';
import { TagModule } from 'primeng/tag';
import { IProjectDeadlineNotification } from '@/models/project-deadline.model';

// Thành công: tag xanh; Lỗi: tag đỏ bấm mở popover nội dung lỗi
@Component({
    selector: 'app-deadline-mail-result-cell',
    imports: [PopoverModule, TagModule],
    template: `
        @if (row().isSuccess) {
            <p-tag value="Thành công" severity="success" [rounded]="true" />
        } @else {
            <button type="button" class="cursor-pointer border-0 bg-transparent p-0" aria-label="Xem nội dung lỗi" (click)="errorPopover.toggle($event)">
                <p-tag value="Lỗi" severity="danger" [rounded]="true" />
            </button>
            <p-popover #errorPopover appendTo="body">
                <div class="max-w-80 text-xs break-words">{{ row().errorMessage || 'Không có nội dung lỗi' }}</div>
            </p-popover>
        }
    `,
})
export class DeadlineMailResultCellComponent {
    row = input.required<IProjectDeadlineNotification>();
    rowIndex = input<number>();
    data = input<unknown>();
}
