import { ChangeDetectionStrategy, Component, ContentChild, EventEmitter, InjectionToken, Input, OnChanges, Output, signal, SimpleChanges, TemplateRef, ViewChild } from '@angular/core';
import { ControlValueAccessor } from '@angular/forms';
import { ShBaseControl } from '../sh-base';
import { Utils } from '../../../utils';
import { Select } from 'primeng/select';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';
import { ShIconRequiredComponent } from '../../sh-icon-required/sh-icon-required';
import { TruncatePipe } from '../../../pipes/truncate.pipe';
const SH_SELECT_VALUE_ACCESSOR = new InjectionToken<ControlValueAccessor>(
    'LibSelectValueAccessor'
);
@Component({
    selector: 'sh-select',
    imports: [CommonModule, FormsModule, SelectModule, TooltipModule, ShIconRequiredComponent, TruncatePipe],
    templateUrl: './sh-select.html',
    styleUrl: './sh-select.scss',
    providers: [
        {
            provide: SH_SELECT_VALUE_ACCESSOR,
            useExisting: ShSelectComponent
        },
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ShSelectComponent extends ShBaseControl implements OnChanges {
    @Input() data: any;
    @Input() itemDisableds: any[] = [];

    private _options = signal<any[]>(null);
    @Input() set options(value) {
        if (value) this._options.set(Utils.cloneData(value));
    }
    get options() {
        return this._options();
    }

    @Input() optionLabel: string;
    @Input() optionValue: string;
    @Input() activeReferences: boolean = false;
    @Input() scrollHeight: string = '200px';
    @Input() filter: boolean = false;
    @Input() style: Object = null;
    @Input() styleClass: string = null;
    @Input() panelStyle: Object = null;
    @Input() panelStyleClass: string = null;
    @Input() readonly: boolean = false;
    @Input() editable: boolean = false;
    @Input() appendTo: string = 'body';
    @Input() filterPlaceholder: string;
    @Input() filterLocale: string;
    @Input() dataKey: string;
    @Input() filterBy: string;
    @Input() filterValue: string;
    @Input() filterMatchMode: "endsWith" | "startsWith" | "contains" | "equals" | "notEquals" | "in" | "lt" | "lte" | "gt" | "gte" = "contains";

    @Input() virtualScroll: boolean = false;
    @Input() virtualScrollItemSize: number;

    @Input() showTooltip: boolean = false;
    @Input() tooltip: string = '';
    @Input() tooltipPosition: "left" | "top" | "bottom" | "right" = "bottom";

    @Input() optionDisabled: string = 'isDisabledItem';
    @Input() groupConfig: any = {
        group: false,
        optionGroupLabel: 'label',
        optionGroupChildren: 'items'
    };
    @Input() showClear: boolean = false;
    @Input() emptyFilterMessage: string = 'Không có dữ liệu';
    @Input() emptyMessage: string = 'Không có dữ liệu';

    @Input() genLabelCode: string[];
    @Input() genLabelCodeSeparator: string = ' - ';

    @Input() resetFilterOnHide: boolean = true;
    @Input() autoDisplayFirst: boolean = false;
    @Input() truncateLength: number;
    @Input() icon: string;
    @Output() onChange = new EventEmitter();
    @Output() onBlur = new EventEmitter();
    @Output() onFilter = new EventEmitter();
    @Output() onFocus = new EventEmitter();
    @Output() onClick = new EventEmitter();
    @Output() onShow = new EventEmitter();
    @Output() onHide = new EventEmitter();
    @Output() onClear = new EventEmitter();
    @Output() onLazyLoad = new EventEmitter();

    @ContentChild('selectedItemTemplate') selectedItemTemplate: TemplateRef<any>;
    @ContentChild('itemTemplate') itemTemplate: TemplateRef<any>;

    @ViewChild('dropdown') dropdown: Select;
    isInit: boolean = true;

    override writeValue(value: any): void {
        super.writeValue(value);
        this.setTooltip(value, true)
    }

    override ngOnChanges(changes: SimpleChanges): void {
        super.ngOnChanges(changes);
        if (changes['data']?.currentValue) {
            if (Array.isArray(this.data) && this.data.length) {
                if (this.activeReferences) {
                    this.options = this.data?.[0];
                } else {
                    this.options = Utils.cloneData(this.data?.[0]);
                }
                this.optionLabel = this.data?.[1];
                this.optionValue = this.data?.[2];
            }
            this.genOptionLabel();
            this.setFirstItem()
        }
        if (!this.optionLabel && !this.optionValue) {
            const firstItem = this.data?.[0]?.[0] || this.options?.[0];
            this.optionLabel = (firstItem?.label && 'label') || (firstItem?.name && 'name');
            this.optionValue = (firstItem?.code && 'code') || (firstItem?.id && 'id');
        }

        if (changes['options']?.currentValue) {
            if (!this.activeReferences) {
                this.options = Utils.cloneData(this.options);
            }
            this.genOptionLabel();
            this.setFirstItem()
        }
        if (changes['tooltip']?.currentValue) {
            this.showTooltip = true;
        }
        this.setVirtualScroll();
    }

    setVirtualScroll() {
        if (this.options?.length > 300) {
            this.virtualScroll = true;
            this.virtualScrollItemSize = this.virtualScrollItemSize || 30;
        }
    }

    ngAfterViewInit() {
        this.dropdown.onFilterInputChange = this.customFilter.bind(this)
    }

    searchTimeOut: any;
    customFilter(event: Event) {
        if (this.searchTimeOut) clearTimeout(this.searchTimeOut);
        this.searchTimeOut = setTimeout(() => {
            const value: string = (event.target as HTMLInputElement).value;
            (event.target as HTMLInputElement).value = value.trim();
            Select.prototype.onFilterInputChange.apply(this.dropdown, [event]);
            this.dropdown.cd.detectChanges();
            (event.target as HTMLInputElement).value = value;
        }, 200)
    }

    genOptionLabel() {
        if (this.genLabelCode) {
            this.optionLabel = 'labelGen';
            this.options = this.options.map((option) => {
                const data: string[] = this.genLabelCode.map(item => {
                    const propertieLevels = item?.split('.');
                    let value: any;
                    propertieLevels.forEach((property, index) => {
                        if (index === 0) {
                            value = option?.[property];
                        } else {
                            value = value?.[property];
                        }
                    });
                    return value;
                })
                option[this.optionLabel] = data.join(this.genLabelCodeSeparator);
                return option;
            })
        }
        this.setTooltip(this.value, true);
    }

    setFirstItem() {
        if (!this.value) {
            if (this.options?.length && this.autoDisplayFirst) {
                const firstItem = this.options[0];
                let value: any;
                if (this.optionValue) {
                    const propertieLevels = this.optionValue?.split('.');
                    propertieLevels.forEach((property, index) => {
                        if (index === 0) {
                            value = firstItem?.[property];
                        } else {
                            value = value?.[property];
                        }
                    });
                } else {
                    value = this.options[0];
                }

                setTimeout(() => {
                    this.value = value;
                    this._onChange(value);
                    this.cd.markForCheck();
                }, 0);
            }
        }
    }

    isShowDropdown: boolean;
    _onShow($event) {
        this.isShowDropdown = true;
    }

    _onHide($event) {
        this.isShowDropdown = false;
    }

    _onClear() {
        this.onClear.emit();
        setTimeout(() => {
            this.dropdown.hide();
        }, 0);
    }

    setTooltip(value, detectChanges?: boolean) {
        if (this.showTooltip) {
            const data = this.options?.find(item => item?.[this.optionValue] === value);
            this.tooltip = data?.[this.optionLabel] || '';
            if (detectChanges) {
                this.cd.detectChanges();
            }
        }
    }

    _handleOnChange(value) {
        this.setTooltip(value)
        this.dropdown.hide()
        this.onChange.emit(value)
    }

}
