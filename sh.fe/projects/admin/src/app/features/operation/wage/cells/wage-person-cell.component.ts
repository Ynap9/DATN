import { Component, computed, input } from '@angular/core';
import { WageRow } from './wage-row';

@Component({
    selector: 'app-wage-person-cell',
    template: `
        <div class="flex items-center gap-2.5">
            <span class="grid h-[30px] w-[30px] shrink-0 place-items-center rounded-full bg-[var(--app-accent-soft)] text-[11px] font-bold text-[var(--app-accent)]">
                {{ initials() }}
            </span>
            <div class="min-w-0">
                <div class="font-semibold" [style.color]="tag() ? 'var(--app-muted)' : null">
                    {{ row().fullName || row().userName }}
                    @if (tag(); as label) {
                        <span class="ml-1.5 whitespace-nowrap rounded border border-[var(--app-line-strong)] px-1.5 align-[1px] text-[11px] font-semibold text-[var(--app-muted)]">{{ label }}</span>
                    }
                </div>
                <div class="text-xs text-[var(--app-muted)]">{{ row().userName }}</div>
            </div>
        </div>
    `,
})
export class WagePersonCellComponent {
    row = input.required<WageRow>();
    rowIndex = input<number>();
    data = input<unknown>();

    // Đã gỡ ưu tiên hơn Đang tắt
    readonly tag = computed(() => (this.row().isRemoved ? 'Đã gỡ' : this.row().isActive ? '' : 'Đang tắt'));
    // Chữ cái đầu của từ đầu và từ cuối họ tên ("Vũ Thanh Tùng" → "VT")
    readonly initials = computed(() => {
        const words = (this.row().fullName || this.row().userName).trim().split(/\s+/);
        const letters = words.length > 1 ? [words[0], words[words.length - 1]] : [words[0]];
        return letters.map((word) => word.charAt(0)).join('').toUpperCase();
    });
}
