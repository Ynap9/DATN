import { Component, input } from '@angular/core';
import { TooltipModule } from 'primeng/tooltip';

// Ô CUSTOM_COMP của DataTable (nhận đủ { row, rowIndex, data }): cắt 1 dòng, rê chuột xem đủ; rỗng → "—"
@Component({
    selector: 'app-package-text-cell',
    imports: [TooltipModule],
    template: `
        @if (data()) {
            <span class="mx-auto block max-w-[260px] truncate" [pTooltip]="data()!" tooltipPosition="top" tooltipStyleClass="max-w-[28rem]">{{ data() }}</span>
        } @else {
            <span class="text-[var(--app-faint)]">—</span>
        }
    `,
})
export class PackageTextCellComponent {
    row = input<unknown>();
    rowIndex = input<number>();
    data = input<string | null>();
}
