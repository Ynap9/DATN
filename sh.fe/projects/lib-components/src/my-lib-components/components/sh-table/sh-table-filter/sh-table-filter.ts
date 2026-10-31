import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, OnInit, Output, Renderer2, SimpleChanges } from '@angular/core';
import { ETableTopBar, IItemFilter } from '../../../shared/interfaces/sh-table.interface';
import { Utils } from '../../../utils';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ShCalendarComponent } from '../../form-inputs/sh-calendar/sh-calendar';
import { ShSelectComponent } from '../../form-inputs/sh-select/sh-select';
import { ShMultiSelectComponent } from '../../form-inputs/sh-multi-select/sh-multi-select';
import { ShInputSearchComponent } from '../../filter/sh-input-search/sh-input-search';
import { ShDropdownSearchComponent } from '../../filter/sh-dropdown-search/sh-dropdown-search';
import { ShButtonComponent } from '../../sh-button/sh-button';

@Component({
    selector: 'sh-table-filter',
    imports: [CommonModule, FormsModule, ShCalendarComponent, ShSelectComponent, ShMultiSelectComponent, ShInputSearchComponent, ShDropdownSearchComponent, ShButtonComponent],
    templateUrl: './sh-table-filter.html',
    styleUrl: './sh-table-filter.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ShTableFilterComponent implements OnInit, OnChanges {
    constructor(
        private renderer: Renderer2,
        private cd: ChangeDetectorRef,
    ) { }

    @Input() items: IItemFilter[] = [];
    @Input() data: any;
    @Input() class: string;
    @Input() widthContentRight: number = 20;
    @Input() cacheName: string;
    @Input() dataFilter: any = {};
    @Output() _onChange = new EventEmitter<any>(null);

    ETableTopBar = ETableTopBar;

    indexLimit: number = 0;
    isShowTopBar: boolean = false;
    isShowOption: boolean = false;
    isSeeMore: boolean = false;

    itemShows: any[] = [];
    itemHiddens: any[] = [];


    wrapperId: string;
    widthClassInputSearchDefault = 'w-21rem';
    widthClassMultiSelectDefault = 'w-21rem';
    widthClassDefault = 'w-30';

    listKeywordTypes: string[] = [];

    ngOnInit(): void {
        this.variableFilterInit();
        this.wrapperId = 'wrapper-table-top-bar' + new Date().getTime();
    }

    ngOnChanges(changes: SimpleChanges): void {

        if (changes['dataFilter'] && changes['dataFilter'].currentValue !== changes['dataFilter'].previousValue) {
            this.dataFilter = { ...changes['dataFilter'].currentValue };
            this.cd.markForCheck();
        }

        if (changes['data']) {
            this.isShowTopBar = false;
            this.cd.detectChanges();
            setTimeout(() => {
                this.isShowTopBar = true;
            }, 0);
        }
        if (changes['items']?.currentValue) {
            const itemInputGroup = this.items.find(item => item.type === ETableTopBar.INPUT_SELECT);
            if (itemInputGroup) {
                this.listKeywordTypes = itemInputGroup.optionConfig?.data?.map(item => item?.[itemInputGroup?.optionConfig.value]);
            }
        }
        if (changes['items']?.currentValue) {
            const itemInputGroup = this.items.find(item => item.type === ETableTopBar.DATE_SELECT);
            if (itemInputGroup) {
                this.listKeywordTypes = itemInputGroup.optionConfig?.data?.map(item => item?.[itemInputGroup?.optionConfig.value]);
            }
        }
    }

    ngAfterViewInit() {
        this.cd.detectChanges();

        const topBar = document.getElementById(this.wrapperId);

        if (!topBar) return;

        const maxWidthContentLeft = topBar.clientWidth * 0.75 * 0.95;
        const itemEls: any = topBar.getElementsByClassName('item');

        let countWidthLimit = 0;

        [...itemEls].forEach((element: any, index: number) => {
            countWidthLimit += element?.clientWidth || 0;
            if (countWidthLimit < maxWidthContentLeft) {
                this.indexLimit = index;
            }
        });
    }

    emitChange() {
        if (this.cacheName) Utils.setSessionStorage(this.cacheName, JSON.stringify(this.dataFilter))
        this._onChange.emit(this.dataFilter);

    }

    variableFilterInit() {
        const sessionStorageFilter = JSON.parse(Utils.getSessionStorage(this.cacheName));
        if (sessionStorageFilter) {
            this.dataFilter = {
                ...sessionStorageFilter,
            };
            this.emitChange()
        } else {
            let isEmitData: boolean = false;
            this.items.forEach((item: IItemFilter) => {
                if ([ETableTopBar.SELECT, ETableTopBar.MULTIPLE_SELECT, ETableTopBar.INPUT_TEXT, ETableTopBar.DATE].includes(item.type)) {
                    if (item?.optionConfig) {
                        this.dataFilter[item.variableReference] = item?.defaultValue ?? null;
                        console.log(item?.defaultValue)
                        if (item?.optionConfig?.showClear === null) item.optionConfig.showClear = true;
                        if (item?.defaultValue) isEmitData = true;
                    }
                }
            });

            if (isEmitData) this.emitChange();
        }

    }

    changeInputGroup(event) {
        let dataFilterClone = { ...this.dataFilter };
        delete dataFilterClone['keyword'];
        delete dataFilterClone['keywordType'];

        for (let key of this.listKeywordTypes) {
            dataFilterClone[key] = '';
        }
        const dataEmit = {
            ...dataFilterClone,
            ...event
        }
        this._onChange.emit(dataEmit);
    }

    changedDateGroup(event) {
        let dataFilterClone = { ...this.dataFilter };
        delete dataFilterClone['date'];
        delete dataFilterClone['typeDate'];

        for (let key of this.listKeywordTypes) {
            dataFilterClone[key] = '';
        }
        const dataEmit = {
            ...dataFilterClone,
            ...event
        }
        this._onChange.emit(dataEmit);
    }

}
