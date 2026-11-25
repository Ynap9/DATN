import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StatusBadge } from '@/shared/components/status-badge/status-badge.component';
import { formatWageNumber, WageRow } from './wage-row';

// data = đơn giá của cột: undefined = không thấy giá (—), null = chưa đặt giá (nhãn vàng, Admin bấm sang Thành viên dự án)
@Component({
    selector: 'app-wage-price-cell',
    imports: [RouterLink, StatusBadge],
    template: `
        <div class="text-right tabular-nums whitespace-nowrap">
            @if (data() === undefined) {
                <span class="text-[var(--app-muted)]">—</span>
            } @else if (data() === null) {
                @if (row().priceLink; as link) {
                    <a routerLink="/admin/operation/project-member" [queryParams]="link" class="no-underline">
                        <app-status-badge class="cursor-pointer" [badge]="missingBadge" />
                    </a>
                } @else {
                    <app-status-badge [badge]="missingBadge" />
                }
            } @else {
                {{ format(data()!) }}<span class="ml-0.5 text-[11px] text-[var(--app-muted)]">đ</span>
            }
        </div>
    `,
})
export class WagePriceCellComponent {
    row = input.required<WageRow>();
    rowIndex = input<number>();
    data = input<number | null | undefined>();

    readonly missingBadge = { label: 'Chưa đặt giá', tone: 'amber' } as const;
    readonly format = formatWageNumber;
}
