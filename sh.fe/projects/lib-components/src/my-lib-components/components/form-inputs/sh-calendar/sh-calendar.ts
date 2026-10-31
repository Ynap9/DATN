import { SH_NG_VALUE_ACCESSOR, ShBaseControl } from './../sh-base';
import { AfterViewInit, ChangeDetectionStrategy, Component, ContentChild, EventEmitter, forwardRef, InjectionToken, Input, OnChanges, OnInit, Output, SimpleChanges, TemplateRef, ViewChild } from '@angular/core';
import moment from 'moment';
import { DatePicker, DatePickerResponsiveOptions, DatePickerTypeView, LocaleSettings } from 'primeng/datepicker';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DatePickerModule } from 'primeng/datepicker';
import { TooltipModule } from 'primeng/tooltip';
import { ShIconRequiredComponent } from '../../sh-icon-required/sh-icon-required';


@Component({
    selector: 'sh-calendar',
    imports: [CommonModule, FormsModule, DatePickerModule, TooltipModule, ShIconRequiredComponent],
    templateUrl: './sh-calendar.html',
    styleUrl: './sh-calendar.scss',
    providers: [
        {
            provide: SH_NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => ShCalendarComponent),
            multi: true
        }
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShCalendarComponent extends ShBaseControl implements OnInit, OnChanges, AfterViewInit {
    @Input() style: Object;
    @Input() styleClass: string;
    @Input() inputStyle: Object;
    @Input() inputStyleClass: string;
    @Input() dateFormat: string = 'dd/mm/yy';
    @Input() valueFormat: string;
    @Input() multipleSeparator: string = ',';
    @Input() rangeSeparator: string = '-';
    @Input() inline: boolean = false;
    @Input() showOtherMonths: boolean = true;
    @Input() selectOtherMonths: boolean = false;
    @Input() showIcon: boolean = true;
    @Input() icon: string;
    @Input() appendTo: string = 'body';
    @Input() readonlyInput: boolean = false;
    @Input() shortYearCutoff: any = '+10';
    @Input() hourFormat: string = '24';
    @Input() timeOnly: boolean = false;
    @Input() stepYearPicker: number = 10;
    @Input() stepHour: number = 1;
    @Input() stepMinute: number = 1;
    @Input() stepSecond: number = 1;
    @Input() showSeconds: boolean = false;
    @Input() showOnFocus: boolean = true;
    @Input() showWeek: boolean = false;
    @Input() startWeekFromFirstDayOfYear: boolean = false;
    @Input() showClear: boolean = false;
    @Input() dataType: string = 'date';
    @Input() selectionMode: "multiple" | "range" | "single" = "single";
    @Input() maxDateCount: number;
    @Input() todayButtonStyleClass: string = 'p-button-text';
    @Input() clearButtonStyleClass: string = 'p-button-text';
    @Input() autoZIndex: boolean = true;
    @Input() baseZIndex: number = 0;
    @Input() panelStyleClass: string;
    @Input() panelStyle: string;
    @Input() keepInvalid: boolean = true;
    @Input() hideOnDateTimeSelect: boolean = false;
    @Input() touchUI: boolean = false;
    @Input() timeSeparator: string = ':';
    @Input() focusTrap: boolean = true;
    @Input() showTransitionOptions: string = '.12s cubic-bezier(0, 0, 0.2, 1)';
    @Input() hideTransitionOptions: string = '.1s linear';
    @Input() tabindex: number;
    @Input() minDate: Date ;
    @Input() maxDate: Date ;
    @Input() disabledDates: Date[];
    @Input() disabledDays: number[];
    @Input() showTime: boolean = false;
    @Input() responsiveOptions: DatePickerResponsiveOptions[];
    @Input() numberOfMonths: number = 1;
    @Input() firstDayOfWeek: number = 7;
    @Input() locale: LocaleSettings;
    @Input() view: DatePickerTypeView;
    @Input() defaultDate: Date;
    @Input() override placeholder: string = null;

    @Input() mask: string;
    @Input() showButtonBar: boolean = false;

    @Output() onFocus = new EventEmitter(null);
    @Output() onBlur = new EventEmitter(null);
    @Output() onClose = new EventEmitter(null);
    @Output() onSelect = new EventEmitter(null);
    @Output() onChange = new EventEmitter(null);
    @Output() onClear = new EventEmitter(null);
    @Output() onClearClick = new EventEmitter(null);
    @Output() onInput = new EventEmitter(null);
    @Output() onTodayClick = new EventEmitter(null);
    @Output() onMonthChange = new EventEmitter(null);
    @Output() onYearChange = new EventEmitter(null);
    @Output() onClickOutside = new EventEmitter(null);
    @Output() onShow = new EventEmitter(null);

    @ContentChild('headerTemplate') headerTemplate: TemplateRef<any>;
    @ContentChild('footerTemplate') footerTemplate: TemplateRef<any>;
    @ContentChild('dateTemplate') dateTemplate: TemplateRef<any>;

    @ViewChild('calendar') calendar: DatePicker;

    override ngOnInit(): void {
        this.onInit();
        if (this.selectionMode === 'single') {
            if (!this.placeholder) {
                let timePlaceholder = `HH:mm${this.showSeconds ? ':ss' : ''}`;
                if (this.timeOnly) {
                    this.placeholder = timePlaceholder;
                } else {
                    if (this.showTime) {
                        this.placeholder = `dd/mm/yyyy ${timePlaceholder}`
                    } else {
                        this.placeholder = 'dd/mm/yyyy';
                    }
                }
            }
            if (!this.mask) {
                let mask = '';
                let timeMask = `99:99${this.showSeconds ? ':99' : ''}`;
                if (this.timeOnly) {
                    mask = timeMask;
                } else {
                    if (this.showTime) {
                        mask = `99/99/9999 ${timeMask}`
                    } else {
                        mask = '99/99/9999';
                    }
                }
                this.cd.detectChanges();
                this.mask = mask;
            }
        }
    }

    ngAfterViewInit(): void {

    }

    override ngOnChanges(changes: SimpleChanges): void {
        super.ngOnChanges(changes);
        if (changes['minDate']?.currentValue) {
            this.minDate = (moment(this.minDate).isValid() && new Date(this.minDate)) || null;
        }
        if (changes['maxDate']?.currentValue) {
            this.maxDate = (moment(this.maxDate).isValid() && new Date(this.maxDate)) || null;
        }
        if (changes['placeholder']) {
            this.placeholder = this.placeholder === undefined ? 'dd/mm/yyyy' : this.placeholder;
        }
        if (changes['value']?.currentValue) {
            this.value = (moment(this.value).isValid() && new Date(this.value)) || null;
        }
    }

    override writeValue(value: any): void {
        let valueFormat: any;
        if (value) {
            if (Array.isArray(value) && value?.length) {
                valueFormat = value.map(date => {
                    return (moment(date).isValid() && new Date(date)) || null;
                })
            } else {
                if (this.timeOnly) {
                    const today = moment(new Date()).format('YYYY-MM-DD');
                    value = today + 'T' + value;
                }
                valueFormat = (moment(value).isValid() && new Date(value)) || null;
            }
        } else {
            valueFormat = null;
        }
        this.value = valueFormat;
        setTimeout(() => {
            if (this.value) this.formatDataToLocalDate(this.value, false, false);
        }, 0);
        this.cd.markForCheck();
    }

    _onSelect(event) {
        console.log('_onSelect', new Date(event).toISOString(), this.value);
        if (this.selectionMode == 'range' && Array.isArray(this.value)) {
            if (this.value[1] != null) {
                this.onSelect.emit(this.formatDataToLocalDate(this.value, true))
            }
        }
        else {
            this.onSelect.emit(this.formatDataToLocalDate(this.value, true))
        }
    }

    _onClearClick(event) {
        this._onChange(null);
        this.onClearClick.emit(event)
        this.onChange.emit(null)
    }

    _onClear(event) {
        this._onChange(null);
        this.onClear.emit(event)
        this.onChange.emit(null)
    }

    isShowDatepicker: boolean = false;
    _onShow(event) {
        this.isShowDatepicker = true;
        const datePickerEl: any = document.querySelector('.p-datepicker');
        const minWidth = getComputedStyle(datePickerEl).getPropertyValue('min-width');
        const minWidthNumber = parseInt(minWidth, 10);
        const widthIcon = 32;
        datePickerEl.style.minWidth = `${((minWidthNumber + widthIcon))}px`;
        this.cd.detectChanges();
    }

    _onClose(event) {
        this.isShowDatepicker = false;
    }


    _onInput(event) {
        console.log('_onInput', event.target.value, this.value);

        if (!this.value) {
            this._onChange(null);
            if (!event?.target?.value) {
                this.onChange.emit(null)
            }
        } else {
            this.formatDataToLocalDate(this.value);
        }
    }

    _onTodayClick(event) {
    }

    formatDataToLocalDate(value, onSelect: boolean = false, isEmit: boolean = true) {
        let valueFormat: any;
        let format = this.valueFormat || 'YYYY-MM-DDTHH:mm:ss';
        if (value) {
            if (Array.isArray(value) && value?.length) {
                valueFormat = value.map(date => {
                    if (!this.showSeconds) date = new Date(date).setSeconds(0);
                    return (date && moment(date).format(format)) || date;
                })
            } else {
                if (!this.showSeconds) value = new Date(value).setSeconds(0);
                if (this.timeOnly) format = `HH:mm${this.showSeconds ? ':ss' : ''}`;
                valueFormat = (value && moment(value).format(format)) || value;
            }
            this._onChange(valueFormat);
            this.cd.markForCheck();
            if (onSelect) this.calendar.hideOverlay();
            if (isEmit) this.onChange.emit(valueFormat);
        }
        return valueFormat;
    }
}
