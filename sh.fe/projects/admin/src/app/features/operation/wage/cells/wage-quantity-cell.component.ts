import { Component, input } from '@angular/core';
import { WAGE_QUANTITY_UNITS } from '@/shared/constants/wage.constants';
import { formatWageNumber, WageRow } from './wage-row';

@Component({
    selector: 'app-wage-quantity-cell',
    template: `
        <div class="text-right tabular-nums">{{ format(row().quantity) }}</div>
        <div class="text-right text-[11px] text-[var(--app-muted)]">{{ units[row().role] }}</div>
    `,
})
export class WageQuantityCellComponent {
    row = input.required<WageRow>();
    rowIndex = input<number>();
    data = input<unknown>();

    readonly units = WAGE_QUANTITY_UNITS;
    readonly format = formatWageNumber;
}
