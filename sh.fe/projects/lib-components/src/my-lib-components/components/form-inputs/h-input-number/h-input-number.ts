import { ChangeDetectionStrategy, Component, EventEmitter, forwardRef, Input, OnChanges, OnInit, Output, SimpleChanges, ViewChild } from '@angular/core';
import { H_NG_VALUE_ACCESSOR, HBaseControl } from '../h-base';
import { InputNumber } from 'primeng/inputnumber';
import { Utils } from '../../../utils';
import { keyNumbers } from '../../../shared/const/base.consts';

@Component({
    selector: 'h-input-number',
    standalone: false,
    templateUrl: './h-input-number.html',
    styleUrl: './h-input-number.scss',
    providers: [
        {
            provide: H_NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => HInputNumberComponent),
            multi: true
        }
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class HInputNumberComponent extends HBaseControl implements OnInit, OnChanges {
    @Input() cursorMovementSupport: boolean = false;
    @Input() showButtons?: boolean = false;
    @Input() format?: boolean = true;
    @Input() buttonLayout?: "stacked" | "horizontal" | "vertical" = 'stacked';
    @Input() styleClass?: string | undefined;
    @Input() style?: Object;
    @Input() size?: 'small' |'large' ='small';
    @Input() maxlength?: number | undefined;
    @Input() tabindex?: number | undefined;
    @Input() title?: string | undefined;
    @Input() autocomplete?: string | undefined = 'off';
    @Input() min?: number | undefined = 0;
    @Input() max?: number | undefined = 999999999999999;
    @Input() incrementButtonClass?: string | undefined;
    @Input() decrementButtonClass?: string | undefined;
    @Input() incrementButtonIcon?: string | undefined;
    @Input() decrementButtonIcon?: string | undefined;
    @Input() readonly?: boolean = false;
    @Input() step: number = 1;
    @Input() allowEmpty?: boolean = true;
    @Input() locale?: string = 'vi-VN';
    @Input() localeMatcher?: "lookup" | "best fit";
    @Input() mode?: 'decimal' | 'currency' = 'decimal';
    @Input() variant?: 'filled' | 'outlined' = 'outlined';
    @Input() currency?: string | undefined;
    @Input() currencyDisplay?: string | undefined;
    @Input() useGrouping?: boolean = true;
    @Input() minFractionDigits?: number | undefined;
    @Input() maxFractionDigits?: number | undefined;
    @Input() isInteger: boolean = true;
    @Input() prefix?: string | undefined;
    @Input() suffix?: string | undefined;
    @Input() inputStyle?: any;
    @Input() inputStyleClass?: string | undefined;
    @Input() showClear?: boolean = false;
    @Input() autofocus?: boolean | undefined;
    @Input() ariaLabel?: string | undefined = "ariaLabel";

    @Output() onInput = new EventEmitter<any>();
    @Output() onFocus = new EventEmitter<any>();
    @Output() onBlur = new EventEmitter<any>();
    @Output() onKeyDown = new EventEmitter<any>();
    @Output() onClear = new EventEmitter<any>();

    @ViewChild('inputNumber') inputNumber: InputNumber;

    decimalSeparator: string = ',';
    thousandSeparator: string = '.';

    cacheValue: number;
    onChangeTimeOut: any;
    inputEl: any;

    selectionEnd: number;
    keyNumberActives: string[];
    keyDown: string;

    count: number = 0;
    override ngOnInit(): void {
        super.ngOnInit()
        setTimeout(() => {
            if (this.max && this.value && this.value > this.max) {
                this.value = this.max;
                this._onChange(this.max);
            }
            this.cacheValue = this.value;
            this.cd.detectChanges();
        }, 0);
    }

    override ngOnChanges(changes: SimpleChanges): void {
        super.ngOnChanges(changes);
        if (changes['locale']?.currentValue) {
            if (['en-US', 'en-IN', 'ja-JP', 'zh-CN'].includes(this.locale)) {
                this.decimalSeparator = '.';
                this.thousandSeparator = ',';
            }
        }

        if (changes['isInteger']?.currentValue === false) {
            if (!Utils.valueExist(this.minFractionDigits)) this.minFractionDigits = 0;
            if (!Utils.valueExist(this.maxFractionDigits)) this.maxFractionDigits = 2;
        }
    }

    onValueChange(event): void {
        let isLimitValue: boolean;
        if (this.onChangeTimeOut) clearTimeout(this.onChangeTimeOut);
        if (this.max) {
            this.onChangeTimeOut = setTimeout(() => {
                if (keyNumbers.includes(this.keyDown)) {
                    var modelValue = event.value;
                    if (this.max && modelValue && modelValue > this.max) {
                        isLimitValue = true;
                        modelValue = this.cacheValue || this.max;
                        this.value = this.cacheValue || this.max
                    }
                    this.cacheValue = Utils.cloneData(modelValue);
                } else {
                    this.value = this.cacheValue;
                    modelValue = this.cacheValue;
                }
                this._onChange(modelValue);
                this.cd.markForCheck();
                if ((this.minFractionDigits || this.maxFractionDigits) && isLimitValue) {
                    setTimeout(() => {
                        this.inputEl.setSelectionRange(this.selectionEnd + 1, this.selectionEnd + 1);
                    }, 0);
                }
            }, 0);
        } else {
            this._onChange(event.value);
        }

    }

    _onKeyDown(event: any) {
        const getFormattedValue = (value) => {
            let formattedValue = Utils.formatCurrency(value);
            if (this.minFractionDigits) {
                if (Number.isInteger(value)) {
                    for (let i = 1; i <= this.minFractionDigits; i++) {
                        if (i === 1) {
                            formattedValue = formattedValue + `${this.decimalSeparator}0`;
                        } else {
                            formattedValue = formattedValue + '0';
                        }
                    }
                }
            }
            return formattedValue;
        }
        const getKeyActives = (formattedValue: string, positionCursor: number, event): string[] => {
            const keyNumberActives = []
            const prefixLength = this.prefix?.length || 0;
            positionCursor = positionCursor - prefixLength;

            if (this.minFractionDigits === 0) {
                if (event.key === this.decimalSeparator) {
                    formattedValue = formattedValue + this.decimalSeparator;
                    positionCursor = positionCursor + 1;
                } else {
                    formattedValue = event.target.value;
                }
            }
            for (const keyNumber of keyNumbers) {
                const formattedValueTest = (formattedValue && formattedValue.slice(0, positionCursor) + keyNumber + formattedValue.slice(positionCursor, formattedValue?.length)) || keyNumber;
                const formattedValueNumber = +formattedValueTest.replaceAll(this.thousandSeparator, '').replaceAll(this.decimalSeparator, '.');
                const checkValueMax = typeof (formattedValueNumber) === 'number' && formattedValueNumber > this.max;
                if (!checkValueMax) {
                    keyNumberActives.push(keyNumber)
                } else {
                    break;
                }
            };
            return keyNumberActives;
        }

        this.keyDown = event.key;
        this.inputEl = event.target;

        if (event.key === 'Backspace') {
            setTimeout(() => {
                if (this.value?.toString()?.length === 1) {
                    this.inputEl.setSelectionRange(1, 1);
                }
            }, 0);
        }

        if (this.max) {
            const key = event.key;
            const input = event.target as HTMLInputElement;
            let value = this.value;
            var formattedValue = getFormattedValue(value);
            this.selectionEnd = input?.selectionEnd || 0;
            let positionCursor: number;
            const moveCursor: boolean = this.selectionEnd === input?.selectionStart;

            if (moveCursor) {
                positionCursor = this.selectionEnd;
                if (event.key && key === 'ArrowLeft') {
                    positionCursor = this.selectionEnd >= 1 && this.selectionEnd - 1;
                }
                else if (event.key && key === 'ArrowRight') {
                    positionCursor = this.selectionEnd <= formattedValue?.length && this.selectionEnd + 1;
                }
                this.keyNumberActives = [];
                this.keyNumberActives = getKeyActives(formattedValue, positionCursor, event);
                if (key) {
                    if (this.cursorMovementSupport) {
                        setTimeout(() => {
                            let formattedValue = getFormattedValue(this.value);
                            const keyNumberActives = getKeyActives(formattedValue, positionCursor + 1, event);
                            const positionCursorDecimalSeparator = formattedValue.search(this.decimalSeparator);
                            if (keyNumberActives.length === 0 && this.minFractionDigits && keyNumbers.includes(key)) {
                                this.inputEl.setSelectionRange(positionCursorDecimalSeparator + 1, positionCursorDecimalSeparator + 1);
                            }
                        }, 0);
                    }
                    else {
                        const positionCursorDecimalSeparator = formattedValue.search(this.decimalSeparator) + (this.prefix?.length || 0);
                        if (key === this.decimalSeparator && positionCursorDecimalSeparator === this.selectionEnd && this.keyNumberActives.length === 0 && this.minFractionDigits) {
                            this.inputEl.setSelectionRange(positionCursorDecimalSeparator + 1, positionCursorDecimalSeparator + 1);
                        }
                    }
                }
            } else {
                this.keyNumberActives = keyNumbers;
            }
            if (['ArrowLeft', 'ArrowRight', 'Backspace', 'Delete', 'Tab', 'ArrowDown', 'ArrowUp', , ...this.keyNumberActives, ...[this.decimalSeparator]].includes(event.key)) {
                this.keyDown = this.keyNumberActives.includes(this.keyDown) ? this.keyDown : '0';
                return;
            }

            if (event.ctrlKey && ['a', 'A', 'v', 'V', 'c', 'C'].includes(event.key)) {
                this.keyDown = this.keyNumberActives.includes(this.keyDown) ? this.keyDown : '0';
                return;
            }
            event.preventDefault();
        }

    }

    onPaste(event: ClipboardEvent) {
        event.preventDefault();
        if (this.value > this.max) {
            this.value = this.cacheValue;
            this._onChange(this.value);
        }
    }
}
