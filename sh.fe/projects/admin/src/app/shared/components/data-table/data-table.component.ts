import { DatePipe, NgComponentOutlet } from '@angular/common';
import { AfterViewInit, Component, computed, ElementRef, inject, InjectionToken, Injector, input, model, OnInit, output, OutputEmitterRef, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DialogService } from 'primeng/dynamicdialog';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { TooltipModule } from 'primeng/tooltip';
import { CurrencyPipe } from '@sh/ui';
import { CellViewTypes, TableEventTypes } from '@/shared/constants/data-table.constants';
import { IColumn, IColumnFilterValue, IColumnSetting, ITableEvent, ITableRowReorder } from '@/shared/models/data-table.models';
import { StatusBadge } from '../status-badge/status-badge.component';
import { TblAction } from '../tbl-action/tbl-action.component';
import { DataTableColumnFilter, hasFilterValue } from './data-table-column-filter.component';
import { DataTableColumnSettings, IColumnSettingsDialogData } from './data-table-column-settings.component';

export const TBL_CUSTOM_COMP_EMIT = new InjectionToken<OutputEmitterRef<ITableEvent>>('TBL_CUSTOM_COMP_EMIT');

const COLUMN_WIDTH_STORAGE_PREFIX = 'tbl_col_widths_';
const COLUMN_SETTING_STORAGE_PREFIX = 'tbl_cols_';

@Component({
    selector: 'app-data-table',
    imports: [
        FormsModule,
        TableModule,
        PaginatorModule,
        TagModule,
        ToggleSwitchModule,
        TooltipModule,
        DatePipe,
        NgComponentOutlet,
        CurrencyPipe,
        TblAction,
        StatusBadge,
        DataTableColumnFilter,
    ],
    templateUrl: './data-table.component.html',
    styleUrl: './data-table.component.scss',
})
export class DataTable implements OnInit, AfterViewInit {
    private readonly _injector = inject(Injector);
    private readonly _elementRef = inject(ElementRef);
    private readonly _dialogService = inject(DialogService);

    columns = input.required<IColumn[]>();
    data = input.required<any[]>();
    dataKey = input('id');
    loading = input(false);
    pageNumber = input(1);
    pageSize = input(25);
    total = input(0);
    tableKey = input('');
    rowsPerPageOptions = input([10, 25, 50, 100]);
    paginator = input(true);
    reorderable = input(false);
    selectable = input(false);
    selection = model<any[]>([]);
    columnSettings = input(false);
    filterValues = input<IColumnFilterValue>({});

    pageChange = output<PaginatorState>();
    action = output<ITableEvent>();
    rowReorder = output<ITableRowReorder>();
    // Phát toàn bộ map lọc sau mỗi lần Áp dụng / Xoá
    filterChange = output<IColumnFilterValue>();

    readonly cellViewTypes = CellViewTypes;
    readonly emptyText = '—';
    readonly customInjector = Injector.create({
        providers: [{ provide: TBL_CUSTOM_COMP_EMIT, useValue: this.action }],
        parent: this._injector,
    });
    private readonly _savedSettings = signal<IColumnSetting[] | null>(null);

    // Bật cấu hình cột: cột khoá giữ chỗ cũ, các cột còn lại xếp theo cấu hình đã lưu và bỏ cột đang ẩn
    readonly visibleColumns = computed(() => {
        const columns = this.columns();
        if (!this.columnSettings()) {
            return columns;
        }
        const settings = this.validSettings(this._savedSettings()) ?? this.defaultSettings();
        const byKey = new Map(columns.map((col) => [this.getColumnKey(col), col]));
        const ordered = settings.map((setting) => byKey.get(setting.key)!);
        const visibleKeys = new Set(settings.filter((setting) => setting.visible).map((setting) => setting.key));
        return columns.map((col) => (col.locked ? col : ordered.shift()!)).filter((col) => col.locked || visibleKeys.has(this.getColumnKey(col)));
    });
    readonly hasFrozen = computed(() => this.visibleColumns().some((col) => col.frozen));

    private columnWidths: Record<string, number> = {};

    ngOnInit(): void {
        this.columnWidths = this.readColumnWidths();
        if (this.columnSettings()) {
            this._savedSettings.set(this.readColumnSettings());
        }
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

    getColumnKey(col: IColumn): string {
        return col.key ?? col.field ?? col.header;
    }

    getProgress(row: any, field = ''): number {
        const value = Number(this.get(row, field)) || 0;
        return Math.round(Math.min(100, Math.max(0, value)));
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

    onFilterApply(col: IColumn, value: unknown): void {
        const next: IColumnFilterValue = { ...this.filterValues() };
        const key = this.getColumnKey(col);
        if (hasFilterValue(value)) {
            next[key] = value;
        } else {
            delete next[key];
        }
        this.filterChange.emit(next);
    }

    openColumnSettings(): void {
        const headers = new Map(this.columns().map((col) => [this.getColumnKey(col), col.header]));
        const withHeader = (settings: IColumnSetting[]) => settings.map((setting) => ({ ...setting, header: headers.get(setting.key)! }));
        const data: IColumnSettingsDialogData = {
            items: withHeader(this.validSettings(this._savedSettings()) ?? this.defaultSettings()),
            defaults: withHeader(this.defaultSettings()),
            lockedHeaders: this.columns().filter((col) => col.locked).map((col) => col.header),
        };
        this._dialogService
            .open(DataTableColumnSettings, { header: 'Cấu hình cột', width: '28rem', modal: true, closable: true, data })
            ?.onClose.subscribe((settings?: IColumnSetting[]) => {
                if (!settings) {
                    return;
                }
                this._savedSettings.set(settings);
                try {
                    localStorage.setItem(COLUMN_SETTING_STORAGE_PREFIX + this.tableKey(), JSON.stringify(settings));
                } catch {
                    // localStorage bị chặn: cấu hình chỉ giữ tới khi tải lại trang
                }
                setTimeout(() => this.applyColumnWidths());
            });
    }

    onColumnResize(event: { element: HTMLElement }): void {
        const key = event.element.dataset['colKey'];
        if (!key || !this.tableKey()) {
            return;
        }
        this.columnWidths[key] = event.element.offsetWidth;
        try {
            localStorage.setItem(COLUMN_WIDTH_STORAGE_PREFIX + this.tableKey(), JSON.stringify(this.columnWidths));
        } catch {
            // localStorage bị chặn (chế độ riêng tư): chỉ mất phần nhớ độ rộng
        }
    }

    // Cache cũ khoá theo vị trí cột (số) thì bỏ qua
    private readColumnWidths(): Record<string, number> {
        try {
            const cached = JSON.parse(localStorage.getItem(COLUMN_WIDTH_STORAGE_PREFIX + this.tableKey()) || '{}');
            return Object.fromEntries(Object.entries(cached).filter(([key, width]) => !/^\d+$/.test(key) && typeof width === 'number')) as Record<string, number>;
        } catch {
            return {};
        }
    }

    private defaultSettings(): IColumnSetting[] {
        return this.columns()
            .filter((col) => !col.locked)
            .map((col) => ({ key: this.getColumnKey(col), visible: !col.hidden }));
    }

    // Cấu hình hợp lệ khi đúng tập key của các cột không khoá hiện tại (thêm / bớt cột thì về mặc định)
    private validSettings(settings: IColumnSetting[] | null): IColumnSetting[] | null {
        const keys = this.defaultSettings().map((setting) => setting.key);
        const isValid =
            Array.isArray(settings) &&
            settings.length === keys.length &&
            settings.every((setting) => typeof setting?.key === 'string' && typeof setting.visible === 'boolean' && keys.includes(setting.key)) &&
            new Set(settings.map((setting) => setting.key)).size === keys.length;
        return isValid ? settings : null;
    }

    private readColumnSettings(): IColumnSetting[] | null {
        try {
            return this.validSettings(JSON.parse(localStorage.getItem(COLUMN_SETTING_STORAGE_PREFIX + this.tableKey()) || 'null'));
        } catch {
            return null;
        }
    }

    private applyColumnWidths(): void {
        const headers: NodeListOf<HTMLElement> = this._elementRef.nativeElement.querySelectorAll('table thead tr th[data-col-key]');
        headers.forEach((th) => {
            const width = this.columnWidths[th.dataset['colKey']!];
            if (width) {
                th.style.width = `${width}px`;
            }
        });
    }
}
