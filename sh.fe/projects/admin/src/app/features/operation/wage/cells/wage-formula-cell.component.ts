import { Component, input } from '@angular/core';
import { formatWageNumber, WageRow } from './wage-row';

// Cột "Cách tính" của nhân viên: khối lượng × đơn giá thực nhận
@Component({
    selector: 'app-wage-formula-cell',
    template: `
        @if (row().actualPrice === null) {
            <span class="text-xs text-[var(--app-muted)]">Liên hệ quản lý dự án để được đặt giá</span>
        } @else {
            <span class="font-mono text-xs text-[var(--app-muted)] whitespace-nowrap">{{ format(row().quantity) }} × {{ format(row().actualPrice!) }}</span>
        }
    `,
})
export class WageFormulaCellComponent {
    row = input.required<WageRow>();
    rowIndex = input<number>();
    data = input<unknown>();

    readonly format = formatWageNumber;
}
