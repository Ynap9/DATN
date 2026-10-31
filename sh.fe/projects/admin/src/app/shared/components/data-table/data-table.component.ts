import { DatePipe, NgComponentOutlet } from '@angular/common';
import { AfterViewInit, Component, ElementRef, inject, InjectionToken, Injector, input, OnInit, output, OutputEmitterRef } from '@angular/core';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { CurrencyPipe } from '@sh/ui';
import { CellViewTypes, TableEventTypes } from '@/shared/constants/data-table.constants';
import { IColumn, ITableEvent } from '@/shared/models/data-table.models';
import { TblAction } from '../tbl-action/tbl-action.component';

export const TBL_CUSTOM_COMP_EMIT = new InjectionToken<OutputEmitterRef<ITableEvent>>('TBL_CUSTOM_COMP_EMIT');

const COLUMN_WIDTH_STORAGE_PREFIX = 'tbl_col_widths_';

@Component({
    selector: 'app-data-table',
    imports: [TableModule, PaginatorModule, TagModule, DatePipe, NgComponentOutlet, CurrencyPipe, TblAction],
    templateUrl: './data-table.component.html',
    styleUrl: './data-table.component.scss',
})
export class DataTable implements OnInit, AfterViewInit {
    private readonly _injector = inject(Injector);
    private readonly _elementRef = inject(ElementRef);

    columns = input.required<IColumn[]>();
    data = input.required<any[]>();
    dataKey = input('id');
    loading = input(false);
    pageNumber = input(1);
    pageSize = input(25);
    total = input(0);
    tableKey = input('');
    rowsPerPageOptions = input([10, 25, 50, 100]);

    pageChange = output<PaginatorState>();
    action = output<ITableEvent>();

    readonly cellViewTypes = CellViewTypes;
    readonly emptyText = '—';
    readonly customInjector = Injector.create({
        providers: [{ provide: TBL_CUSTOM_COMP_EMIT, useValue: this.action }],
        parent: this._injector,
    });

    private columnWidths: Record<number, number> = {};

    ngOnInit(): void {
        this.columnWidths = this.readColumnWidths();
    }

    ngAfterViewInit(): void {
        setTimeout(() => this.applyColumnWidths());
    }

    get(row: any, field = ''): any {
        return field.split('.').reduce((value, key) => value?.[key], row);
    }

    getText(row: any, field = ''): any {
        const value = this.get(row, field);
        return value === null || value === undefined || value === '' ? this.emptyText : value;
    }

    getIndex(rowIndex: number): number {
        return (this.pageNumber() - 1) * this.pageSize() + rowIndex + 1;
    }

    isAllChecked(field = ''): boolean {
        const rows = this.data();
        return rows.length > 0 && rows.every((row) => this.get(row, field));
    }

    onHeaderCheckbox(col: IColumn): void {
        this.action.emit({ type: TableEventTypes.HEADER_CHECKBOX, field: col.field });
    }

    onCellClick(col: IColumn, row: any): void {
        this.action.emit({ type: TableEventTypes.CELL_CLICK, field: col.field, row });
    }

    onColumnResize(event: { element: HTMLElement }): void {
        const th = event.element;
        const index = Array.from(th.parentElement?.children ?? []).indexOf(th);
        if (index >= 0 && this.tableKey()) {
            this.columnWidths[index] = th.offsetWidth;
            localStorage.setItem(COLUMN_WIDTH_STORAGE_PREFIX + this.tableKey(), JSON.stringify(this.columnWidths));
        }
    }

    private readColumnWidths(): Record<number, number> {
        try {
            return JSON.parse(localStorage.getItem(COLUMN_WIDTH_STORAGE_PREFIX + this.tableKey()) || '{}');
        } catch {
            return {};
        }
    }

    private applyColumnWidths(): void {
        const headers: NodeListOf<HTMLElement> = this._elementRef.nativeElement.querySelectorAll('table thead tr th');
        headers.forEach((th, index) => {
            if (this.columnWidths[index]) {
                th.style.width = `${this.columnWidths[index]}px`;
            }
        });
    }
}
