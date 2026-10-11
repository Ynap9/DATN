import { ChangeDetectionStrategy, Component, EventEmitter, forwardRef, InjectionToken, Input, OnChanges, Output, SimpleChanges, ViewChild } from '@angular/core';
import { ControlValueAccessor } from '@angular/forms';
import { H_NG_VALUE_ACCESSOR, HBaseControl } from '../h-base';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';

@Component({
    selector: 'h-input-text',
    standalone: false,
    templateUrl: './h-input-text.html',
    styleUrl: './h-input-text.scss',
    providers: [
        {
            provide: H_NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => HInputTextComponent),
            multi: true,
        }
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class HInputTextComponent extends HBaseControl implements OnChanges {

    @Input() type: string = 'text';
    @Input() maxlength: string;

    @Input() textarea: boolean;
    @Input() rows: number = 1;
    @Input() autoResize: boolean = false;
    @Input() autocomplete: 'on' | 'off' = 'off';

    @Input() showTooltip: boolean = false;
    @Input() tooltip: string = '';
    @Input() tooltipPosition: "left" | "top" | "bottom" | "right" = "bottom";

    @Output() onInput = new EventEmitter<any>();
    @Output() blur = new EventEmitter<any>();
    @Output() keyup = new EventEmitter<any>();

    @ViewChild('inputText') inputText: InputText;
    @ViewChild('inputTextarea') inputTextarea: Textarea;

    firstValue: any;
    isInit: boolean = true;

    override writeValue(value: any): void {
        if (this.isInit && !value) this.firstValue = value;
        this.value = value ? value.trim() : null;
        this.isInit = false;
        this.cd.markForCheck();
    }

    override ngOnChanges(changes: SimpleChanges): void {
        super.ngOnChanges(changes);
        if (changes['tooltip']?.currentValue) {
            this.showTooltip = true;
        }
    }
    _onResize($event: {} | Event) {

    }
}
