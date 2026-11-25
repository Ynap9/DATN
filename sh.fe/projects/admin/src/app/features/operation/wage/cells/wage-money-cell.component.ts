import { Component, input } from '@angular/core';
import { formatWageNumber, WageRow } from './wage-row';

// data = thành tiền của cột; null = người xem không thấy giá công ty; 0 hiện màu phụ (thường do chưa đặt giá)
@Component({
    selector: 'app-wage-money-cell',
    template: `
        <div class="text-right tabular-nums whitespace-nowrap">
            @if (data() === null || data() === undefined) {
                <span class="text-[var(--app-muted)]">—</span>
            } @else {
                <span [style.color]="data() ? null : 'var(--app-muted)'">{{ format(data()!) }}</span>
                <span class="ml-0.5 text-[11px] font-normal text-[var(--app-muted)]">đ</span>
            }
        </div>
    `,
})
export class WageMoneyCellComponent {
    row = input.required<WageRow>();
    rowIndex = input<number>();
    data = input<number | null>();

    readonly format = formatWageNumber;
}
