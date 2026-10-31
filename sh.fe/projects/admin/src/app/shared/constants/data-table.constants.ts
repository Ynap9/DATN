export const CellViewTypes = {
    INDEX: 'INDEX',
    DATE: 'DATE',
    CURRENCY: 'CURRENCY',
    CHECKBOX: 'CHECKBOX',
    LINK_BLANK: 'LINK_BLANK',
    STATUS: 'STATUS',
    ACTIONS: 'ACTIONS',
    CUSTOM_COMP: 'CUSTOM_COMP',
} as const;

export type CellViewType = (typeof CellViewTypes)[keyof typeof CellViewTypes];

export const TableEventTypes = {
    CELL_CLICK: 'cellClick',
    HEADER_CHECKBOX: 'headerCheckbox',
} as const;
