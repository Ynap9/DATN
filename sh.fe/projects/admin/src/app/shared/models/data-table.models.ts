import { Type } from '@angular/core';
import { CellViewType } from '@/shared/constants/data-table.constants';

export type TagSeverity = 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast';

export interface ITableAction {
    type: string;
    label: string;
    icon?: string;
}

export interface ITableEvent<T = any> {
    type: string;
    row?: T;
    field?: string;
}

export interface IColumn<T = any> {
    header: string;
    field?: string;
    headerContainerStyle?: string;
    cellClass?: string;
    cellStyle?: string;
    cellViewType?: CellViewType;
    dateFormat?: string;
    clickable?: boolean;
    actions?: ITableAction[];
    customComponent?: Type<unknown>;
    statusSeverity?: (row: T) => TagSeverity;
}
