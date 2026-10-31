import { booleanAttribute, ChangeDetectorRef, Component, ElementRef, EventEmitter, Input, OnDestroy, OnInit, Output, SimpleChanges, ViewChild, ViewChildren, QueryList } from '@angular/core';
import { trigger, state, style, transition, animate } from '@angular/animations';
import { NgxSpinnerService } from 'ngx-spinner';
import { DialogService } from 'primeng/dynamicdialog';
import { BaseConsts, DefaultImage, EFormatDate, EFormatDateDisplay, EYesNo } from '../../shared/const/base.consts';
import { Page } from '../../shared/models/page';
import { Utils } from '../../utils';
import { IAction, IColumn } from '../../shared/interfaces/sh-table.interface';
import { EImageTable, ETableColumnType, ETableFrozen, ValueType } from '../../shared/const/sh-table.const';
import { Paginator } from 'primeng/paginator';
import { Subject } from 'rxjs';
import moment from 'moment';
import { ShSettingDisplayColumnTableComponent } from './sh-setting-display-column-table/sh-setting-display-column-table';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { CheckboxModule } from 'primeng/checkbox';
import { SelectModule } from 'primeng/select';
import { PopoverModule } from 'primeng/popover';
import { PaginatorModule } from 'primeng/paginator';
import { MenuModule } from 'primeng/menu';
import { TagModule } from 'primeng/tag';
import { NgxSpinnerModule } from 'ngx-spinner';
import { NgClass, NgFor, NgIf, NgStyle, NgSwitch, NgSwitchCase, NgSwitchDefault, NgTemplateOutlet } from '@angular/common';
import { ShCheckboxComponent } from '../sh-checkbox/sh-checkbox';
import { ShButtonComponent } from '../sh-button/sh-button';
import { FunctionPipe } from '../../pipes/function.pipe';
import { CurrencyPipe } from '../../pipes/currency.pipe';

@Component({
    selector: 'sh-table',
    imports: [NgClass, NgFor, NgIf, NgStyle, NgSwitch, NgSwitchCase, NgSwitchDefault, NgTemplateOutlet, FormsModule, TableModule, CheckboxModule, SelectModule, PopoverModule, PaginatorModule, MenuModule, TagModule, NgxSpinnerModule, ShCheckboxComponent, ShButtonComponent, FunctionPipe, CurrencyPipe],
    templateUrl: './sh-table.html',
    styleUrl: './sh-table.scss',
    animations: [
        trigger('animationRefresh', [
            transition(':enter', [
                style({ opacity: 0 }),
                animate('300ms ease-in', style({ opacity: 1 }))
            ])
        ])
    ]
})
export class ShTableComponent implements OnInit, OnDestroy {
    constructor(
        private dialogService: DialogService,
        private ref: ChangeDetectorRef,
        private spinner: NgxSpinnerService,
        private cdr: ChangeDetectorRef,
    ) {
    }

    @Input() rows: any[] = [];
    @Input() class: string;
    @Input() id: string;
    @Input() styleClassPaginator: string = "show-option-all";
    @Input() isLoading: boolean = false;
    @Input() rowHover: boolean = true;
    @Input() responsiveLayout: string = 'scroll';
    @Input() dataKey: string = 'id';
    @Input() reorderableColumns: boolean = false;
    @Input() showCurrentPageReport: boolean = true;
    @Input() paginator: boolean = false;
    @Input() scrollable: boolean = true;
    @Input() additionalHeight: number = 0;
    @Input() columnCacheName: string;
    @Input() serverConfig: boolean = false;
    @Input() columns: IColumn[] = [];
    @Input() listAction: IAction[][] = [];
    @Input() resetBorder: boolean = false;
    @Input() emptyDataMessage: string;

    @Input() onChangeAtributionData: boolean = true;
    @Input() fieldContentHtmls: string[] = [];


    @Input() perPageOptions: number[];
    @Input() page: Page = new Page('sh-table');
    @Output() pageChange = new EventEmitter<Page>();

    @Input() selectedItems: any[] = [];
    @Output() selectedItemsChange = new EventEmitter<any>();
    @Output() _onChangeCheckbox = new EventEmitter<any>();
    @Output() _onChangeCheckboxAll = new EventEmitter<any>();
    @Output() onRowReorder = new EventEmitter<any>();

    @Input() isShowPaginator: boolean = true;
    @Input() isShowIconFilter: boolean = true;
    @Input() isShowFilter: boolean = true;
    @Output() isShowFilterChange = new EventEmitter<boolean>();

    @Output() _onPageChange = new EventEmitter<any>();
    @Output() _onSort = new EventEmitter<any>();
    @Output() selectOption = new EventEmitter<any>();
    @Output() _onColumnFilter = new EventEmitter<Record<string, string>>();
    @Output() _onColumnConfigChange = new EventEmitter<IColumn[]>();
    @Output() _onColumnConfigReset = new EventEmitter<void>();

    @Input() tabContentHeight: number = 0;
    @Input() idHeader: string;
    @Input() scrollHeightConst: number = 0;
    @Input() isHeightAuto: boolean = false;
    @Input() showTable: boolean = true;
    @Input({ transform: booleanAttribute }) tableInTab: boolean = false;
    @Input() isDisabledActionDropdown = true;
    @Input() disabled: boolean = false;

    @Input() updateSetHeight: boolean = false;
    @Input() selectedKey: string;
    @Input() maxHeight: string;
    @Input() clickableRow: boolean = false;
    @Output() _onRowClick = new EventEmitter<any>();
    top: any
    left: any
    columnShows: any[] = [];
    permissibleErrorScrollHeight: number = 10;
    divFilterHeight: number = 0;
    scrollHeight: number = 0;
    tableHeight: number;
    @Input() heightConst: number;

    @Input({ transform: booleanAttribute }) isFixHeight: boolean = false;
    @Input() fixHeightBottomGap: number = 16;
    @Input() minFixHeight: number = 160;
    fixHeightPx: number = 0;

    @Input() emptyIcon: string = 'pi pi-inbox';
    @Input() emptyText: string = 'Chưa có dữ liệu';

    isRowOdds: boolean = false;
    isShowTable: boolean = false;
    onResize: any = false;
    isShowLibLoading: boolean;
    api: string;

    iconResizeColumn = 'share/assets/layout/images/icon-resize-column.png';
    private rootFontSizePx = typeof document !== 'undefined'
        ? (parseFloat(getComputedStyle(document.documentElement).fontSize) || 16)
        : 16;

    YesNo = EYesNo;

    ETableColumnType = ETableColumnType;
    ETableFrozen = ETableFrozen;
    EFormatDate = EFormatDate;
    EImageTable = EImageTable
    DefaultImage = DefaultImage;

    @ViewChild('shTable') eTable: ElementRef<HTMLElement>;
    @ViewChild('wrapperPaginator') wrapperPaginator: ElementRef<HTMLElement>;
    @ViewChild('paginatorEl', { static: false }) paginatorEl: Paginator;
    keywordSubject = new Subject<string>();

    sortData: any[] = [];

    columnOriginals: IColumn[] = [];
    setHeightComplete: boolean = false;
    BaseConsts = BaseConsts;
    @ViewChild('checkbox') checkbox: ElementRef;

    optionPage = [
        { name: 'Tất cả', code: true },
        { name: '1 trang', code: false },
    ];

    variableNameRerenders = {
        isShowTable: 'isShowTable',
        isShowPaginator: 'isShowPaginator'
    }

    typeDates: string[] = Object.values(EFormatDateDisplay);
    selectedOption = [];
    Utils = Utils;

    rowItemClone: any;
    showImageEmpty: boolean = false;
    isInit: boolean = true;
    isShowOptionPagePopup: boolean = false;

    activeFilterCol: IColumn | null = null;
    activeFilterValue: any = '';
    activeFilterType: 'text' | 'select' = 'text';
    activeFilterOptions: Array<{ label: string; value: any }> = [];
    columnFilterValues: Record<string, any> = {};
    @ViewChildren('colFilterPopover') colFilterPopovers: QueryList<any>;


    ngOnInit(): void {
        if (!this.id) this.id = 'wrapper-table-' + new Date().getTime();
    }

    extractTextFromHTML(htmlString: string): string {
        const tempElement = document.createElement('div');
        tempElement.innerHTML = htmlString;
        return tempElement.innerText || tempElement.textContent || '';
    }

    onChange(event) {
        if (event) {
            this.selectOption.emit(event)
            setTimeout(() => {
                this.isShowOptionPagePopup = false;
                this.selectedOption = [];
            }, 300);
        }
    }
    onCheckboxClick(event) {
        this.isShowOptionPagePopup = event?.checked;
    }

    checkValidColumnCaches(columnCaches): IColumn[] {
        if (columnCaches) {
            if (columnCaches.length !== this.columns.length) {
                localStorage.removeItem(this.columnCacheName);
                return [];
            } else {
                let isValid = true;
                for (let i = 0; i < columnCaches.length; i++) {
                    if (columnCaches[i].field !== this.columns[i].field) {
                        isValid = false;
                        localStorage.removeItem(this.columnCacheName);
                        break;
                    }
                }
                if (isValid) {
                    return columnCaches;
                }
                return [];
            }
        }
        return [];
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes?.['columns']) {
            this.columnOriginals = [...changes?.['columns'].currentValue];
            this.setAtributionColumn(changes?.['columns'].currentValue);
            let columnCaches = this.serverConfig ? [] : this.checkValidColumnCaches(Utils.getLocalStorage(this.columnCacheName));
            if (columnCaches?.length) {
                let columnCacheNews = this.columns.map((column, index) => {
                    return {
                        ...column,
                        isShow: !column.hasUpdateIsShow ? columnCaches[index].isShow : column.isShow,
                        customWidthRem: columnCaches[index].customWidthRem ?? column.customWidthRem,
                    }
                });
                this.setLocalStorageColumns(columnCacheNews);
                this.columns = columnCacheNews;
            }
            this.setDistanceFrozenColumn();
        }

        if (changes?.['rows']) {

            if (this.rows?.length > 200) {
                Utils.log('Bảng đang render số dòng lớn', this.rows.length);
            }

            if (this.rows?.length && this.rows?.length < this.page.perPageOptions[0]) {
                this.isRowOdds = !(this.rows?.length % 2 === 0);
            }


            if (changes?.['rows']?.currentValue?.length <= changes?.['rows']?.previousValue?.length) {
                const rowCurrentValue = changes?.['rows']?.currentValue?.map(item => item?.[this.dataKey]);
                const rowPreviousValue = changes?.['rows']?.previousValue?.map(item => item?.[this.dataKey]);
                if (!Utils.compareData(rowCurrentValue, rowPreviousValue)) {
                    this.resetPositionScrollBar();
                }
            }

            if (changes?.['rows']?.currentValue?.length) {
                this.rowItemClone = Utils.cloneData(this.rows)[0];
                this.rows = Utils.cloneData(this.rows).map((row) => {
                    if (row) {
                        for (let col of this.columns) {
                            let value: any;
                            value = this.getValue(row, col, col.valueType);
                            if (this.typeDates.includes(col?.type)) {
                                value = (moment(value).isValid() && value) ? moment(value).format(col?.type) : ''
                            }
                            if (col?.type === ETableColumnType.STATUS) {
                                value = this.columnOriginals[col.position].getTagInfo(value);
                            }
                            if (col?.type === ETableColumnType.CHECKBOX_CHANGE) {
                                const valueTemp = value
                                if (col?.valueType === ValueType.ROW) {
                                    value = {
                                        value: valueTemp,
                                        isDisableCheckbox: this.columnOriginals[col.position]?.getDisabled(row?.status)?.isDisableCheckbox,
                                    }
                                } else {
                                    value = {
                                        value: valueTemp,
                                        isDisableCheckbox: false,
                                    }
                                }
                            }

                            if (col?.type === ETableColumnType.ACTION_BUTTON) {
                                const valueTemp = value
                                if (col?.valueType === ValueType.ROW) {
                                    value = {
                                        value: valueTemp,
                                        isDisableButton: this.columnOriginals[col.position]?.disableButtonAction && this.columnOriginals[col.position]?.disableButtonAction(row),
                                        isLoadingButton: this.columnOriginals[col.position]?.loadingButtonAction && this.columnOriginals[col.position]?.loadingButtonAction(row),
                                    }
                                } else {
                                    value = {
                                        value: valueTemp,
                                        isDisableButton: false,
                                        isLoadingButton: this.columnOriginals[col.position]?.loadingButtonAction && this.columnOriginals[col.position]?.loadingButtonAction(row),
                                    }
                                }
                            }
                            if (row[col.field] !== value) {
                                row[col.field + '_customValue'] = value;
                            }
                        }
                        return row;
                    }
                    return {};
                })
            }
            if (!changes['rows'].firstChange) {
                setTimeout(() => {
                    this.showImageEmpty = true;
                }, 100);
            }

        }


        if (changes?.['isLoading']) {
            if (changes?.['isLoading']?.currentValue) {
                this.spinner.show();
                this.showImageEmpty = false;
            } else {
                this.spinner.hide();
                this.showImageEmpty = true;
            }
        }

        if (changes?.['showTable']) {
            if (changes?.['showTable']?.currentValue) {
                this.countDifferenceHeight();
            }
        }

        if (changes?.['heightConst']) {
            if (changes?.['heightConst']?.currentValue) {
                this.countDifferenceHeight();
            }
        }

        if (this.isFixHeight && (changes?.['rows'] || changes?.['showTable'] || changes?.['isLoading'] || changes?.['isShowFilter'])) {
            setTimeout(() => this.computeFixHeight(), 0);
        }
    }


    resetPositionScrollBar() {
        const wrapperTableEl = document.getElementById(this.id);
        if (wrapperTableEl) wrapperTableEl.scrollTo(0, 0);
    }

    ngAfterViewInit() {
        this.countDifferenceHeight();
        if (this.isFixHeight && typeof window !== 'undefined') {
            window.addEventListener('resize', this.boundComputeFixHeight);
            setTimeout(() => this.computeFixHeight(), 0);
        }
    }


    setAtributionColumn(columns) {
        this.columns = columns.map((col: IColumn, index) => {
            return {
                ...col,
                position: index,
                isShow: !(col?.isShow === false),
                hasUpdateIsShow: col?.isShow !== undefined,
                isSort: !!col?.isSort,
                type: col?.type || ETableColumnType.TEXT,
                cutText: col?.isCutText ? `cut-text-${col?.width}` : '',
                displaySettingColumn: !(col?.displaySettingColumn === false),
                isPermision: !(col?.isPermission === false),
                class: (
                    ([ETableColumnType.CHECKBOX_ACTION, ETableColumnType.CHECKBOX_SHOW, ETableColumnType.CHECKBOX_CHANGE,
                    ETableColumnType.ACTION_BUTTON, ETableColumnType.ACTION_ICON,
                    ETableColumnType.IMAGE, EFormatDateDisplay.DATE_DMY, EFormatDateDisplay.DATE_DMY_Hm, EFormatDateDisplay.DATE_DMY_Hms
                    ].includes(col?.type) && ('justify-center text-center'))
                    || (col?.type === ETableColumnType.CURRENCY && ('justify-end text-right'))
                    || (col?.type === ETableColumnType.ACTION_DROPDOWN)
                )
                    + ' ' + col?.class,
            };
        });
    }

    getStyleColumn = (column: IColumn) => {
        const customRem = column.customWidthRem ? column.customWidthRem + 'rem' : null;
        const width    = column.width;
        const minWidth = column.width || column.minWidth;
        return {
            'width':     customRem || (width    ? width    + 'rem' : ''),
            'max-width': customRem || (width    ? width    + 'rem' : ''),
            'min-width': customRem || (minWidth ? minWidth + 'rem' : ''),
            'right':    column?.right != null ? column.right + 'px' : null,
            'left':     column?.left  != null ? column.left  + 'px' : null,
            'position': column?.isFrozen ? 'sticky' : '',
            'background': column?.isFrozen ? 'inherit' : '',
        };
    }

    getValue(row: any, column: IColumn, valueType: ValueType) {
        let property: string = column.field;
        let value: string | number | boolean = row[property?.trim()];
        let propertiesColumn = Object.getOwnPropertyNames(this.columnOriginals[column.position]);
        if (property.includes('||') || property.includes('.') && value === undefined) {
            let properties = property.split('||');
            for (property of properties) {
                property = property?.trim();
                if (property.includes('.')) {
                    let properties = property.split('.');
                    let propertyValue: any = row[properties[0]];
                    if (this.isObject(propertyValue)) {
                        properties.forEach((propertyChild, index) => {
                            if (index > 0 && this.isObject(propertyValue)) {
                                propertyValue = propertyValue[propertyChild];
                            }
                        });
                        value = propertyValue;
                    }
                } else {
                    value = row[property];
                }
                if (value) break;
            }
        }
        if (propertiesColumn.includes('customValue')) {
            let param: any = value;
            if (valueType === ValueType.ROW) {
                param = row;
            }
            value = this.columnOriginals[column.position].customValue(param);
        }
        return value;
    }

    isObject(value) {
        return (typeof value === 'object') && value !== null;
    }

    paginatorChange(event?: any) {
        if (event) {
            this.page.pageNumber = event.page;
            this.page.pageSize = event.rows;
            this.resetPositionScrollBar()
            this.pageChange.emit(this.page);
            if (this.page.pageSize !== this.page.pageSizeAll) {
                this._onPageChange.emit(this.page);
            } else {
                this.resetPositionScrollBar();
                this.loadMore();
            }
        }
    }

    countDifferenceHeight() {

    }

    get wrapperTableStyle() {
        if (this.isFixHeight && this.fixHeightPx > 0) {
            return {
                height: this.fixHeightPx + 'px',
                'overflow-y': 'auto',
                'overflow-x': 'auto',
            };
        }
        return {
            height: this.heightConst ? this.heightConst + 'px' : '',
            'max-height': this.maxHeight || '',
            'overflow-y': this.maxHeight ? 'auto' : '',
        };
    }

    private boundComputeFixHeight = () => this.computeFixHeight();

    private computeFixHeight() {
        if (!this.isFixHeight) return;
        if (typeof window === 'undefined') return;
        const el = this.wrapperETable?.nativeElement;
        if (!el) return;
        const top = el.getBoundingClientRect().top;
        const paginatorH = this.isShowPaginator
            ? (this.wrapperPaginator?.nativeElement?.offsetHeight || 0)
            : 0;
        const available = window.innerHeight
            - top
            - paginatorH
            - (this.fixHeightBottomGap || 0)
            - (this.additionalHeight || 0);
        this.fixHeightPx = Math.max(Math.floor(available), this.minFixHeight);
        this.cdr.markForCheck();
    }

    reRenderHtml(variableName: string) {
        this[variableName] = false;
        this.ref.detectChanges();
        this[variableName] = true;
        if (this.page.pageSize === this.page.pageSizeAll) {
            this.renameLabelPageSizeAll();
        }
    }

    loadMore() {
        this.renameLabelPageSizeAll();
        this._onPageChange.emit();
    }

    renameLabelPageSizeAll() {
        setTimeout(() => {
            try {
                const elementWrapperPaginator = this.wrapperPaginator.nativeElement;
                const elementLabelPageSize = elementWrapperPaginator.querySelector(".p-dropdown-label");
                elementLabelPageSize.textContent = "Tất cả";

                this.ref.detectChanges();
            } catch { }
        });
    }
    @ViewChild('wrapperETable') wrapperETable: ElementRef<HTMLElement>;
    scrollHeightMaxBefore: number = 0;
    onScroll(event) {
        const elementTable = this.wrapperETable.nativeElement;
        let pos = (elementTable.scrollTop) + elementTable.offsetHeight;
        let max = elementTable.scrollHeight;
        if ((pos >= (max - 150)) && (max > this.scrollHeightMaxBefore || this.page.pageNumberLoadMore === 1) && this.rows.length < this.page.totalItems && this.page.pageSize === this.page.pageSizeAll && !this.isLoading) {
            this.scrollHeightMaxBefore = max;
            this.loadMore();
        }
    }

    setColumn() {
        this.dialogService.open(
            ShSettingDisplayColumnTableComponent,
            {
                header: "Cài đặt hiển thị",
                width: '420px',
                styleClass: 'dialog-setcolumn',
                data: {
                    cols: this.columns,
                    showReset: this.serverConfig,
                },
            }
        ).onClose.subscribe((response) => {
            if (response?.reset && this.serverConfig) {
                this._onColumnConfigReset.emit();
                return;
            }
            if (response?.accept && response?.data && Array.isArray(response.data) && response.data.length > 0) {
                this.columns = response.data;
                this.setDistanceFrozenColumn();
                if (this.serverConfig) {
                    this._onColumnConfigChange.emit(this.columns);
                } else {
                    this.setLocalStorageColumns(this.columns);
                }
            }
        });
    }

    private getColumnWidthPx(item: IColumn): number {
        if (item.customWidthRem) return item.customWidthRem * this.rootFontSizePx;
        const remVal = item.width || item.minWidth || 0;
        return remVal * 16;
    }

    setDistanceFrozenColumn() {
        let rightValueStyleFrozenColumn = 0;
        let leftValueStyleFrozenColumn = 0;
        this.columns = this.columns.map((item: IColumn) => {
            if (item?.isFrozen && item?.alignFrozen == ETableFrozen.LEFT && item.isShow) {
                item.left = leftValueStyleFrozenColumn;
                leftValueStyleFrozenColumn += this.getColumnWidthPx(item);
            }
            return item;
        });
        this.columns = this.columns.reverse().map((item: IColumn) => {
            if (item?.isFrozen && item?.alignFrozen === ETableFrozen.RIGHT && item.isShow) {
                item.right = rightValueStyleFrozenColumn;
                rightValueStyleFrozenColumn += this.getColumnWidthPx(item);
            }
            return item;
        }).reverse();

        this.columnShows = this.columns.filter(col => col?.isShow);
        this.colspan = this.columnShows?.length;
        this.reRenderHtml(this.variableNameRerenders.isShowTable);
    }

    colspan: number;
    setLocalStorageColumns(data) {
        this.cdr.markForCheck();
        return Utils.setLocalStorage(this.columnCacheName, data);
    }


    onSelectedChange() {
        let emittedData = this.selectedItems.filter(row => !row?.isDisabled);
        if (this.selectedKey) {
            emittedData = emittedData.map(item => item[this.selectedKey]);
        }
        this.selectedItemsChange.emit(emittedData);
        this._onChangeCheckbox.emit(emittedData);
    }

    changeDisplayFilter() {
        this.isShowFilter = !this.isShowFilter;
        this.scrollHeight = !this.isShowFilter ? (this.scrollHeight + this.divFilterHeight) : (this.scrollHeight - this.divFilterHeight);
        this.isShowFilterChange.emit(this.isShowFilter);
    }

    onSort(event: any) {
        if (JSON.stringify(this.sortData) != JSON.stringify(event?.multisortmeta)) {
            this.sortData = [];
            event?.multisortmeta?.forEach(meta => {
                this.sortData.push({
                    field: meta.field,
                    order: meta.order
                });
            });
            this._onSort.emit(this.sortData);
        }
    }


    counter(i: number) {
        i = i > 1 ? Math.ceil(i) : 0;
        return new Array(i);
    }

    get skeletonRowCount(): number {
        const fit = Math.floor((this.tableHeight || 0) / 38) - 2;
        return Math.max(5, Number.isFinite(fit) ? fit : 0);
    }

    openColumnFilter(event: MouseEvent, col: IColumn, popover: any) {
        event.stopPropagation();
        const original = this.columnOriginals[col.position] || col;
        this.activeFilterCol = original;
        this.activeFilterType = original?.filterType === 'select' ? 'select' : 'text';
        this.activeFilterOptions = original?.filterOptions || [];
        this.activeFilterValue = this.columnFilterValues[col.field] ?? '';
        popover.toggle(event);
    }

    applyColumnFilter(popover: any) {
        if (this.activeFilterCol) {
            const value = typeof this.activeFilterValue === 'string'
                ? this.activeFilterValue.trim()
                : this.activeFilterValue;
            this.columnFilterValues[this.activeFilterCol.field] = value;
            this._onColumnFilter.emit({ ...this.columnFilterValues });
        }
        popover.hide();
    }

    onSelectFilterChange(popover: any) {
        this.applyColumnFilter(popover);
    }

    clearColumnFilter(popover: any) {
        if (this.activeFilterCol) {
            this.columnFilterValues[this.activeFilterCol.field] = '';
            this.activeFilterValue = '';
            this._onColumnFilter.emit({ ...this.columnFilterValues });
        }
        popover.hide();
    }

    hasActiveFilter(field: string): boolean {
        return !!this.columnFilterValues[field];
    }

    ngOnDestroy() {
        this.spinner.hide();
        if (typeof window !== 'undefined') {
            window.removeEventListener('resize', this.boundComputeFixHeight);
        }
    }

    clickCheckBox(row) {
        this._onChangeCheckbox.emit(row);
    }

    clickCheckBoxAll() {
        this._onChangeCheckboxAll.emit();
    }

    showActions() {
    }

    _onRowReorder(event: any) {
        const rowPropertyOriginals = this.rows.map((item) => {
            const newItem: any = {};
            for (let property in this.rowItemClone) {
                newItem[property] = item[property];
            }
            return newItem;
        });
        this.onRowReorder.emit(rowPropertyOriginals);
    }


    preventOverlay(event: MouseEvent): void {
        event.stopPropagation();
    }

    trackByRow = (index: number, item: any): any => {
        return (item && this.dataKey && item[this.dataKey] != null) ? item[this.dataKey] : index;
    };

    onBodyRowClick(row: any, event: MouseEvent) {
        if (!this.clickableRow || !row) return;

        const target = event.target as HTMLElement;
        const interactiveSelector = [
            'button',
            'a',
            'input',
            'textarea',
            'select',
            '.p-checkbox',
            '.p-radiobutton',
            '.p-menu',
            '.p-menuitem',
            '.p-dropdown',
            '.p-multiselect',
            '.button-actions',
            '.pi-ellipsis-h',
            '.b-icon',
            '[pReorderableRowHandle]',
        ].join(',');

        if (target?.closest(interactiveSelector)) return;
        this._onRowClick.emit(row);
    }

    onRowSelect(any){

    }
}
