import { ChangeDetectionStrategy, Component, EventEmitter, forwardRef, InjectionToken, Input, OnChanges, Output, SimpleChanges, ViewChild } from '@angular/core';
import { ControlValueAccessor } from '@angular/forms';
import { SH_NG_VALUE_ACCESSOR, ShBaseControl } from '../sh-base';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { TooltipModule } from 'primeng/tooltip';
import { ShIconRequiredComponent } from '../../sh-icon-required/sh-icon-required';

@Component({
    selector: 'sh-input-text',
    imports: [CommonModule, FormsModule, InputTextModule, TextareaModule, TooltipModule, ShIconRequiredComponent],
    templateUrl: './sh-input-text.html',
    styleUrl: './sh-input-text.scss',
    providers: [
        {
            provide: SH_NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => ShInputTextComponent),
            multi: true,
        }
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ShInputTextComponent extends ShBaseControl implements OnChanges {

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
