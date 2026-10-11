export enum ETableColumnType {
    REORDER = "REORDER",
    IMAGE = "IMAGE",
    TEXT = 'TEXT',
    CHECKBOX_ACTION = 'CHECKBOX_ACTION',
    CHECKBOX_CHANGE = 'CHECKBOX_CHANGE',
    CHECKBOX_SHOW = 'CHECKBOX_SHOW',
    CURRENCY = 'CURRENCY',
    ACTION_DROPDOWN = 'ACTION_DROPDOWN',
    ACTION_BUTTON = 'ACTION_BUTTON',
    ACTION_ICON = 'ACTION_ICON',
    STATUS = 'STATUS',
    HISTORY = 'HISTORY',

    LINK = 'LINK',
}

export enum ETableFrozen {
    LEFT = 'left',
    RIGHT = 'right',
}

export enum ValueType {
    FIELD = 'FIELD',
    ROW = 'ROW',
}

export enum ETableMessage {
    empty = 'Không có dữ liệu',
}

export enum OrderSort {
    ASC = 1,
    DESC = -1,
}


export enum EImageTable {
    EMPTY = '/shared/assets/layout/images/table/empty-2.png',
}
