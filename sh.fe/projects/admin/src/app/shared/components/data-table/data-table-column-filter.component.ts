import { Component, computed, input, output, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ShCalendarComponent, ShSelectComponent } from '@sh/ui';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { Popover, PopoverModule } from 'primeng/popover';
import { TooltipModule } from 'primeng/tooltip';
import { IColumn, IDateRangeValue } from '@/shared/models/data-table.models';

// Rỗng = chuỗi trống, null / undefined, hoặc khoảng ngày không có đầu nào
export function hasFilterValue(value: unknown): boolean {
    if (value === null || value === undefined || value === '') {
        return false;
    }
    if (typeof value === 'object') {
        const range = value as IDateRangeValue;
        return !!range.from || !!range.to;
    }
    return true;
}

@Component({
    selector: 'app-data-table-column-filter',
    imports: [FormsModule, ButtonModule, InputTextModule, PopoverModule, TooltipModule, ShCalendarComponent, ShSelectComponent],
    templateUrl: './data-table-column-filter.component.html',
    styleUrl: './data-table-column-filter.component.scss',
})
export class DataTableColumnFilter {
    private readonly _popover = viewChild.required<Popover>('popover');

    column = input.required<IColumn>();
    value = input<unknown>();

    // null = xoá lọc của cột
    apply = output<unknown>();

    readonly active = computed(() => hasFilterValue(this.value()));

    text = '';
    from: string | null = null;
    to: string | null = null;

    get invalidRange(): boolean {
        return !!this.from && !!this.to && this.from > this.to;
    }

    toggle(event: MouseEvent): void {
        event.stopPropagation();
        const value = this.value();
        this.text = typeof value === 'string' ? value : '';
        this.from = (value as IDateRangeValue | undefined)?.from ?? null;
        this.to = (value as IDateRangeValue | undefined)?.to ?? null;
        this._popover().toggle(event);
    }

    applyDraft(): void {
        if (this.invalidRange) {
            return;
        }
        const value =
            this.column().filter?.type === 'dateRange' ? { from: this.from || undefined, to: this.to || undefined } : this.text.trim();
        this.emit(value);
    }

    emit(value: unknown): void {
        this.apply.emit(hasFilterValue(value) ? value : null);
        this._popover().hide();
    }
}
