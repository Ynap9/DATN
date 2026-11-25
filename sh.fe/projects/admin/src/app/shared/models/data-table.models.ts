import { Type } from '@angular/core';
import { CellViewType } from '@/shared/constants/data-table.constants';

export type TagSeverity = 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast';

export type BadgeTone = 'blue' | 'amber' | 'green' | 'red' | 'gray';

export interface ITableBadge {
    label: string;
    tone: BadgeTone;
}

export interface IColumnFilter {
    type: 'text' | 'select' | 'dateRange';
    options?: { label: string; value: unknown }[];
}

export interface IDateRangeValue {
    from?: string;
    to?: string;
}

// key cột → giá trị lọc: chuỗi (text), giá trị option (select), IDateRangeValue (dateRange)
export interface IColumnFilterValue {
    [key: string]: unknown;
}

export interface IColumnSetting {
    key: string;
    visible: boolean;
}

export interface ITableAction {
    type: string;
    label: string;
    icon: string;
    severity?: 'secondary' | 'danger';
    visible?: (row: any) => boolean;
}

export interface ITableEvent<T = any> {
    type: string;
    row?: T;
    field?: string;
}

export interface IColumn<T = any> {
    header: string;
    field?: string;
    // Khoá ổn định để nhớ độ rộng / cấu hình cột; mặc định = field, cột không field thì = header
    key?: string;
    frozen?: 'left' | 'right';
    filter?: IColumnFilter;
    // Luôn hiện, không kéo trong dialog cấu hình cột
    locked?: boolean;
    // Ẩn mặc định khi bật cấu hình cột
    hidden?: boolean;
    // Các cột liền nhau cùng group gộp chung một ô tiêu đề nhóm ở hàng trên; không cột nào có group thì không có hàng đó
    group?: string;
    headerContainerStyle?: string;
    cellClass?: string;
    cellStyle?: string;
    cellViewType?: CellViewType;
    dateFormat?: string;
    clickable?: boolean;
    actions?: ITableAction[];
    customComponent?: Type<unknown>;
    statusSeverity?: (row: T) => TagSeverity;
    disabled?: (row: T) => boolean;
    progressTooltip?: (row: T) => string;
    badge?: (row: T) => ITableBadge;
}

export interface ITableRowReorder {
    dragIndex: number;
    dropIndex: number;
}
