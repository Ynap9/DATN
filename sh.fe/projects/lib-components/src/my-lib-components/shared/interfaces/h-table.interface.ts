import { ETableColumnType, ETableFrozen, OrderSort, ValueType } from './../const/h-table.const';
import { EFormatDateDisplay } from "../const/base.consts";
import { MenuItem } from 'primeng/api';

export interface IColumn {
    field?: string;
    header?: string;
    title?: string,
    width?: number;
    minWidth?: number;
    style?: Object;
    left?: number;
    right?: number;
    isShow?: boolean,
    isShowFunction?: (row: any) => boolean,
    hasUpdateIsShow?: boolean,
    isSort?: boolean,
    type?: ETableColumnType | EFormatDateDisplay;
    otherType?: ETableColumnType | EFormatDateDisplay;
    isPin?: boolean;
    isFrozen?: boolean;
    alignFrozen?: ETableFrozen;
    isResize?: boolean;
    class?: string;
    icon?: string;
    classButton?: string;
    isCutText?: boolean;
    position?: number;
    displaySettingColumn?: boolean;
    unit?: string;
    fieldSort?: string;
    getTagInfo?: Function;
    getDisabled?: Function;
    action?: Function;
    customValue?: Function;
    valueType?: ValueType;
    sliceString?: number;
    isPermission?: boolean;
    heightImage?: number;
    widthImage?: number;
    isCheckboxIcon?: boolean;
    isHideOptionCheckbox?: boolean;
    disableButtonAction?: Function;
    loadingButtonAction?: Function;
    label?: string;
    labelFunction?: (row: any) => string;
    maxWidth?: number;
    isFilter?: boolean;
    filterType?: 'text' | 'select';
    filterOptions?: Array<{ label: string; value: any }>;
    customWidthRem?: number;
}

export interface IColumnConfig {
    field?: string;
    header?: string;
    width?: number;
    action?: Function;
    isFrozen?: boolean;
    isPin?: boolean;
    alignFrozen?: ETableFrozen;
    otherType?: ETableColumnType | EFormatDateDisplay;
    type?: ETableColumnType | EFormatDateDisplay;
    customValue?: Function;
    displaySettingColumn?: boolean;
    class?: string;
    getTagInfo?: Function;
    isLink?: boolean
}
export class IAction {
    data?: any;
    label: string;
    icon?: string;
    command: ($event: any) => void;
}

export interface ISort {
    field: string;
    order: OrderSort;
}

export interface IDropdown {
    name?: string;
    labelName?: string;
    code?: number | string | boolean;
    value?: number | string | boolean;
    severity?: string;
    rawData?: any;
}




export interface IEvent {
    originalEvent: PointerEvent;
}

export interface IEventChecked extends IEvent {
    checked: boolean,
}

export interface IEventTabView extends IEvent {
    index: number;
}

export interface IEventData<Data> extends IEvent {
    value: Data;
}

export interface ITabView {
    [key: string]: boolean;
}

export interface ISelectedFileUpload extends IEvent {
    currentFiles: File[],
    files: number;
    originEvent: PointerEvent,
}


export enum ETableTopBar {
    INPUT_SELECT = 'INPUT_SELECT',
    INPUT_TEXT = 'INPUT_TEXT',
    ADD_MULTIPLE = 'ADD_MULTIPLE',
    ADD_SINGLE = 'ADD_SINGLE',
    ACTION_BUTTON = 'ACTION_BUTTON',
    SELECT = 'SELECT',
    MULTIPLE_SELECT = 'MULTIPLE_SELECT',
    DATE = 'DATE',
    DATE_SELECT = 'DATE_SELECT',
    DATE_TO_DATE = 'DATE_TO_DATE',
}

export interface IItemFilter {
    type: ETableTopBar;
    label?: string;
    icon?: string;
    styleClass?: string;
    items?: MenuItem[];
    data?: any;
    command?: Function;
    isShow?: boolean;
    variableReference?: string,
    placeholder?: string,
    widthClass?: string,
    defaultValue?: any,
    optionConfig?: {
        showClear?: boolean,
        data?: any,
        label?: string;
        value?: string,
        filter?: boolean,
        filterBy?: string,
        genLabelCode?: string[]
    };
}

export const SEED_DATA_FILTER_BAR = [
]

