import { Component, computed, input } from '@angular/core';
import { MEMBER_ROLE } from '@/shared/constants/project-member.constants';
import { formatWageNumber, WageRow } from './wage-row';

// Chỉ dòng QA: thanh 96px, phần xanh = lượt Đạt / tổng lượt chấm, phần còn lại đỏ; chưa chấm thì thanh xám
@Component({
    selector: 'app-wage-pass-fail-cell',
    template: `
        @if (row().role !== qaRole) {
            <div class="text-right text-[var(--app-muted)]">—</div>
        } @else {
            <div class="flex flex-col items-end gap-1">
                <div class="flex h-1.5 w-24 overflow-hidden rounded" [style.background]="row().reviewCount ? 'var(--app-red)' : 'var(--app-line)'">
                    <div class="h-full bg-[var(--app-green)]" [style.width.%]="passedPercent()"></div>
                </div>
                <div class="whitespace-nowrap text-[11px] text-[var(--app-muted)] tabular-nums">
                    @if (row().reviewCount) {
                        <b class="font-semibold text-[var(--app-ink)]">{{ format(row().passedCount) }}</b> / {{ format(row().failedCount) }} · {{ passedPercent() }}% đạt
                    } @else {
                        Chưa chấm
                    }
                </div>
            </div>
        }
    `,
})
export class WagePassFailCellComponent {
    row = input.required<WageRow>();
    rowIndex = input<number>();
    data = input<unknown>();

    readonly qaRole = MEMBER_ROLE.QA;
    readonly format = formatWageNumber;
    readonly passedPercent = computed(() => {
        const { passedCount, reviewCount } = this.row();
        return reviewCount ? Math.round((passedCount / reviewCount) * 100) : 0;
    });
}
