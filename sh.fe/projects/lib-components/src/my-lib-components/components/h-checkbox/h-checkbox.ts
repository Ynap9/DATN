import { Component, EventEmitter, Input, OnInit, Output, SimpleChanges } from "@angular/core";
import { CheckboxConsts } from "../../shared/const/base.consts";
@Component({
    selector: 'h-checkbox',
    standalone:false,
    templateUrl: './h-checkbox.html',
    styleUrl: './h-checkbox.scss',
})
export class HCheckboxComponent {
    constructor() { }

    @Input() id: string;
    @Input() styleClass: string;
    @Input() type: string = CheckboxConsts.TYPE_YESNO;
    @Input() readonly: boolean = false;
    @Input() disabled: boolean = false;
    @Input() isCheckboxIcon: boolean = true;

    @Input() value: boolean | string;
    @Output() valueChange = new EventEmitter<boolean | string>();
    @Output() _onChange = new EventEmitter<boolean | string>();

    checked: boolean;
    checkedValues = CheckboxConsts.getCheckedValues();

    ngOnInit(): void {

    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes?.['value']) {
            this.checked = this.checkedValues.includes(changes['value']?.currentValue);
        }
    }

    ngAfterViewInit() {

    }

    onChange(event) {
        let valueType = CheckboxConsts.values.find(c => c.type === this.type);
        this.value = valueType[event.checked ? CheckboxConsts.CHECKED : CheckboxConsts.UNCHECKED];
        this.valueChange.emit(this.value);
        this._onChange.emit(event)
    }
}
